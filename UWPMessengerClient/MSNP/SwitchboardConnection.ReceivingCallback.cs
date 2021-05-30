using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Web;
using Windows.UI.Core;

namespace UWPMessengerClient.MSNP
{
    public partial class SwitchboardConnection
    {
        private string currentResponse;
        public ObservableCollection<Message> MessageList { get; set; } = new ObservableCollection<Message>();
        public event EventHandler NewMessage;
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler PrincipalInvited;

        public void ReceivingCallback(IAsyncResult asyncResult)
        {
            SwitchboardConnection switchboardConnection = (SwitchboardConnection)asyncResult.AsyncState;
            int bytesReceived = switchboardConnection.sbSocket.StopReceiving(asyncResult);
            switchboardConnection.OutputString = Encoding.UTF8.GetString(switchboardConnection.OutputBuffer, 0, bytesReceived);
            string[] responses = switchboardConnection.OutputString.Split("\r\n");
            for (var i = 0; i < responses.Length; i++)
            {
                string[] responseParameters = responses[i].Split(" ");
                switchboardConnection.currentResponse = responses[i];
                try
                {
                    switchboardConnection.commandHandlers[responseParameters[0]]();
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }
                catch (Exception e)
                {
                    var task = switchboardConnection.AddToErrorLog($"{responseParameters[0]} processing error: " + e.Message);
                }
            }
            if (bytesReceived > 0)
            {
                sbSocket.BeginReceiving(OutputBuffer, new AsyncCallback(ReceivingCallback), switchboardConnection);
            }
        }

        private void SeparateAndProcessCommandFromResponse(string response, int payloadSize)
        {
            if (response.Contains("\r\n"))
            {
                response = response.Split("\r\n", 2)[1];
            }
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            byte[] payloadBytes = new byte[payloadSize];
            Buffer.BlockCopy(responseBytes, 0, payloadBytes, 0, payloadSize);
            string payload = Encoding.UTF8.GetString(payloadBytes);
            string newCommand = response.Replace(payload, "");
            if (newCommand != "")
            {
                OutputString = newCommand;
                string[] commandParameters = newCommand.Split(" ");
                commandHandlers[commandParameters[0]]();
            }
        }

        private string SeparatePayloadFromResponse(string response, int payloadSize)
        {
            string payload_response = response;
            if (response.Contains("\r\n"))
            {
                payload_response = response.Split("\r\n", 2)[1];
            }
            byte[] responseBytes = Encoding.UTF8.GetBytes(payload_response);
            byte[] payloadBytes = new byte[payloadSize];
            Buffer.BlockCopy(responseBytes, 0, payloadBytes, 0, payloadSize);
            string payload = Encoding.UTF8.GetString(payloadBytes);
            return payload;
        }

        private void HandleUsr()
        {
            string[] usrParameters = currentResponse.Split(" ");
            if (usrParameters[2] != "OK")
            {
                Connected = false;
            }
            else
            {
                Connected = true;
            }
        }

        private void HandleAns()
        {
            string[] ansParameters = currentResponse.Split(" ");
            if (ansParameters[2] != "OK")
            {
                Connected = false;
            }
            else
            {
                Connected = true;
            }
        }

        private void HandleCal()
        {
            string[] calParameters = currentResponse.Split(" ");
            SessionID = calParameters[3];
            PrincipalInvited?.Invoke(this, new EventArgs());
        }

        private void HandleMsg()
        {
            string[] msgResponses = OutputString.Split("\r\n");
            string[] msgParameters = msgResponses[0].Split(" ");
            string senderDisplayName = msgParameters[2];
            string lengthString = msgParameters[3];
            int messageLength;
            int.TryParse(lengthString, out messageLength);
            string messagePayload = SeparatePayloadFromResponse(OutputString, messageLength);
            string[] messagePayloadParameters = messagePayload.Split("\r\n");
            string[] firstHeaderParameters = messagePayloadParameters[0].Split(" ");
            string[] secondHeaderParameters = messagePayloadParameters[1].Split(" ");
            Action msmsgscontrolAction = new Action(() =>
            {
                //first parameter of the third header in the payload
                switch (messagePayloadParameters[2].Split(" ")[0])
                {
                    case "TypingUser:":
                        var task = ShowTypingUser();
                        break;
                }
            });
            Dictionary<string, Action> contentTypeDictionary = new Dictionary<string, Action>()
            {
                {"text/plain;", () => AddMessage(messagePayloadParameters[4], PrincipalInfo, UserInfo) },
                {"text/x-msmsgscontrol", msmsgscontrolAction },
                {"text/x-msnmsgr-datacast", () => HandleDatacast(messagePayload) },
                {"application/x-ms-ink", () => HandleInk(messagePayload) }
            };
            switch (firstHeaderParameters[0])
            {
                case "Message-ID:":
                    HandleInkChunk(messagePayload);
                    break;
            }
            switch (secondHeaderParameters[0])
            {
                case "Content-Type:":
                    contentTypeDictionary[secondHeaderParameters[1]]();
                    break;
            }
            SeparateAndProcessCommandFromResponse(OutputString, messageLength);
        }

        private void AddMessage(string messageText, UserInfo sender, UserInfo receiver)
        {
            Message newMessage = new Message()
            {
                MessageText = messageText,
                Sender = sender.DisplayName,
                Receiver = receiver.DisplayName,
                SenderEmail = sender.Email,
                ReceiverEmail = receiver.Email,
                IsHistory = false
            };
            NullTypingUser();
            AddToMessageListAndDatabase(newMessage);
            MessageReceived?.Invoke(this, new MessageEventArgs() { message = newMessage });
        }

        private void AddMessage(Message message)
        {
            NullTypingUser();
            AddToMessageListAndDatabase(message);
            MessageReceived?.Invoke(this, new MessageEventArgs() { message = message });
        }

        private void AddToMessageListAndDatabase(Message message)
        {
            var task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                NullTypingUser();
                MessageList.Add(message);
                if (KeepMessagingHistory)
                {
                    DatabaseAccess.AddMessageToTable(UserInfo.Email, PrincipalInfo.Email, message);
                }
                NewMessage?.Invoke(this, new EventArgs());
            });
        }

        private void AddToMessageList(Message message)
        {
            var task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MessageList.Add(message);
                NewMessage?.Invoke(this, new EventArgs());
            });
        }

        public void HandleDatacast(string messagePayload)
        {
            string[] messagePayloadParameters = messagePayload.Split("\r\n");
            switch (messagePayloadParameters[3])
            {
                case "ID: 1":
                    ShowNudge();
                    break;
            }
        }

        public void HandleInk(string messagePayload)
        {
            string[] messagePayloadParameters = messagePayload.Split("\r\n");
            Message inkMessage = new Message()
            {
                MessageText = $"{PrincipalInfo.DisplayName} sent you ink",
                SenderEmail = PrincipalInfo.Email,
                Receiver = UserInfo.DisplayName,
                ReceiverEmail = UserInfo.Email
            };
            if (messagePayloadParameters.Length > 4)
            {
                string messageId = messagePayloadParameters[2].Split(" ")[1];
                string chunksString = messagePayloadParameters[3].Split(" ")[1];
                int.TryParse(chunksString, out int chunks);
                string inkChunk = messagePayloadParameters[5];
                inkMessage.ReceiveFirstInkChunk(chunks, messageId, inkChunk);
            }
            else
            {
                inkMessage.ReceiveSingleInk(messagePayloadParameters[3]);
            }
            NullTypingUser();
            AddToMessageList(inkMessage);
        }

        public void HandleInkChunk(string messagePayload)
        {
            string[] messagePayloadParameters = messagePayload.Split("\r\n");
            string messageId = messagePayloadParameters[0].Split(" ")[1];
            string chunkString = messagePayloadParameters[1].Split(" ")[1];
            int.TryParse(chunkString, out int chunkNumber);
            string encodedChunk = messagePayloadParameters[3];
            var inkMessageQuery = from inkMessage in MessageList
                                    where inkMessage.InkMessageID == messageId
                                    select inkMessage;
            foreach (Message inkMessage in inkMessageQuery)
            {
                inkMessage.ReceiveInkChunk(chunkNumber, encodedChunk);
            }
        }

        public async Task ShowTypingUser()
        {
            var typingTask = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PrincipalInfo.UserIsTyping = $"{HttpUtility.UrlDecode(PrincipalInfo.DisplayName)} is typing...";
            });
            await Task.Delay(6000);
            var nullTask = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PrincipalInfo.UserIsTyping = null;
            });
        }

        public void NullTypingUser()
        {
            var nullTask = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PrincipalInfo.UserIsTyping = null;
            });
        }

        public void ShowNudge()
        {
            string nudgeText = $"{HttpUtility.UrlDecode(PrincipalInfo.DisplayName)} sent you a nudge!";
            Message newMessage = new Message()
            {
                MessageText = nudgeText,
                Receiver = UserInfo.DisplayName,
                SenderEmail = PrincipalInfo.Email,
                ReceiverEmail = UserInfo.Email,
                IsHistory = false
            };
            NullTypingUser();
            AddMessage(newMessage);
        }
    }
}
