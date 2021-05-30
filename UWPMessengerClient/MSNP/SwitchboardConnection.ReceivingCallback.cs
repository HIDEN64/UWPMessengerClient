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
            int bytes_received = switchboardConnection.sbSocket.StopReceiving(asyncResult);
            switchboardConnection.OutputString = Encoding.UTF8.GetString(switchboardConnection.OutputBuffer, 0, bytes_received);
            string[] responses = switchboardConnection.OutputString.Split("\r\n");
            for (var i = 0; i < responses.Length; i++)
            {
                string[] res_params = responses[i].Split(" ");
                switchboardConnection.currentResponse = responses[i];
                try
                {
                    switchboardConnection.commandHandlers[res_params[0]]();
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }
                catch (Exception e)
                {
                    var task = switchboardConnection.AddToErrorLog($"{res_params[0]} processing error: " + e.Message);
                }
            }
            if (bytes_received > 0)
            {
                sbSocket.BeginReceiving(OutputBuffer, new AsyncCallback(ReceivingCallback), switchboardConnection);
            }
        }

        protected void SeparateAndProcessCommandFromResponse(string response, int payload_size)
        {
            if (response.Contains("\r\n"))
            {
                response = response.Split("\r\n", 2)[1];
            }
            byte[] response_bytes = Encoding.UTF8.GetBytes(response);
            byte[] payload_bytes = new byte[payload_size];
            Buffer.BlockCopy(response_bytes, 0, payload_bytes, 0, payload_size);
            string payload = Encoding.UTF8.GetString(payload_bytes);
            string new_command = response.Replace(payload, "");
            if (new_command != "")
            {
                OutputString = new_command;
                string[] cmd_params = new_command.Split(" ");
                commandHandlers[cmd_params[0]]();
            }
        }

        protected string SeparatePayloadFromResponse(string response, int payload_size)
        {
            string payload_response = response;
            if (response.Contains("\r\n"))
            {
                payload_response = response.Split("\r\n", 2)[1];
            }
            byte[] response_bytes = Encoding.UTF8.GetBytes(payload_response);
            byte[] payload_bytes = new byte[payload_size];
            Buffer.BlockCopy(response_bytes, 0, payload_bytes, 0, payload_size);
            string payload = Encoding.UTF8.GetString(payload_bytes);
            return payload;
        }

        protected void HandleUSR()
        {
            string[] usr_params = currentResponse.Split(" ");
            if (usr_params[2] != "OK")
            {
                Connected = false;
            }
            else
            {
                Connected = true;
            }
        }

        protected void HandleANS()
        {
            string[] ans_params = currentResponse.Split(" ");
            if (ans_params[2] != "OK")
            {
                Connected = false;
            }
            else
            {
                Connected = true;
            }
        }

        protected void HandleCAL()
        {
            string[] cal_params = currentResponse.Split(" ");
            SessionID = cal_params[3];
            PrincipalInvited?.Invoke(this, new EventArgs());
        }

        protected void HandleMSG()
        {
            string[] MSG_Responses = OutputString.Split("\r\n");
            string[] MSGParams = MSG_Responses[0].Split(" ");
            string senderDisplayName = MSGParams[2];
            string length_str = MSGParams[3];
            int msg_length;
            int.TryParse(length_str, out msg_length);
            string msg_payload = SeparatePayloadFromResponse(OutputString, msg_length);
            string[] MSGPayloadParams = msg_payload.Split("\r\n");
            string[] FirstHeaderParams = MSGPayloadParams[0].Split(" ");
            string[] SecondHeaderParams = MSGPayloadParams[1].Split(" ");
            Action msmsgscontrolAction = new Action(() =>
            {
                //first parameter of the third header in the payload
                switch (MSGPayloadParams[2].Split(" ")[0])
                {
                    case "TypingUser:":
                        var task = ShowTypingUser();
                        break;
                }
            });
            Dictionary<string, Action> ContentTypeDictionary = new Dictionary<string, Action>()
            {
                {"text/plain;", () => AddMessage(MSGPayloadParams[4], PrincipalInfo, UserInfo) },
                {"text/x-msmsgscontrol", msmsgscontrolAction },
                {"text/x-msnmsgr-datacast", () => HandleDatacast(msg_payload) },
                {"application/x-ms-ink", () => HandleInk(msg_payload) }
            };
            switch (FirstHeaderParams[0])
            {
                case "Message-ID:":
                    HandleInkChunk(msg_payload);
                    break;
            }
            switch (SecondHeaderParams[0])
            {
                case "Content-Type:":
                    ContentTypeDictionary[SecondHeaderParams[1]]();
                    break;
            }
            SeparateAndProcessCommandFromResponse(OutputString, msg_length);
        }

        protected void AddMessage(string message_text, UserInfo sender, UserInfo receiver)
        {
            Message newMessage = new Message()
            {
                MessageText = message_text,
                Sender = sender.displayName,
                Receiver = receiver.displayName,
                SenderEmail = sender.Email,
                ReceiverEmail = receiver.Email,
                IsHistory = false
            };
            NullTypingUser();
            AddToMessageListAndDatabase(newMessage);
            MessageReceived?.Invoke(this, new MessageEventArgs() { message = newMessage });
        }

        protected void AddMessage(Message message)
        {
            NullTypingUser();
            AddToMessageListAndDatabase(message);
            MessageReceived?.Invoke(this, new MessageEventArgs() { message = message });
        }

        protected void AddToMessageListAndDatabase(Message message)
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

        protected void AddToMessageList(Message message)
        {
            var task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MessageList.Add(message);
                NewMessage?.Invoke(this, new EventArgs());
            });
        }

        public void HandleDatacast(string msg_payload)
        {
            string[] MSGPayloadParams = msg_payload.Split("\r\n");
            switch (MSGPayloadParams[3])
            {
                case "ID: 1":
                    ShowNudge();
                    break;
            }
        }

        public void HandleInk(string msg_payload)
        {
            string[] MSGPayloadParams = msg_payload.Split("\r\n");
            Message InkMessage = new Message()
            {
                MessageText = $"{PrincipalInfo.displayName} sent you ink",
                SenderEmail = PrincipalInfo.Email,
                Receiver = UserInfo.displayName,
                ReceiverEmail = UserInfo.Email
            };
            if (MSGPayloadParams.Length > 4)
            {
                string message_id = MSGPayloadParams[2].Split(" ")[1];
                string chunks_str = MSGPayloadParams[3].Split(" ")[1];
                int.TryParse(chunks_str, out int chunks);
                string ink_chunk = MSGPayloadParams[5];
                InkMessage.ReceiveFirstInkChunk(chunks, message_id, ink_chunk);
            }
            else
            {
                InkMessage.ReceiveSingleInk(MSGPayloadParams[3]);
            }
            NullTypingUser();
            AddToMessageList(InkMessage);
        }

        public void HandleInkChunk(string msg_payload)
        {
            string[] MSGPayloadParams = msg_payload.Split("\r\n");
            string message_id = MSGPayloadParams[0].Split(" ")[1];
            int chunk_number;
            string chunk_str = MSGPayloadParams[1].Split(" ")[1];
            int.TryParse(chunk_str, out chunk_number);
            string encoded_chunk = MSGPayloadParams[3];
            var ink_message_query = from ink_message in MessageList
                                    where ink_message.InkMessageID == message_id
                                    select ink_message;
            foreach (Message ink_message in ink_message_query)
            {
                ink_message.ReceiveInkChunk(chunk_number, encoded_chunk);
            }
        }

        public async Task ShowTypingUser()
        {
            var Typing_Task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PrincipalInfo.typingUser = $"{HttpUtility.UrlDecode(PrincipalInfo.displayName)} is typing...";
            });
            await Task.Delay(6000);
            var Null_Task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PrincipalInfo.typingUser = null;
            });
        }

        public void NullTypingUser()
        {
            var Null_Task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PrincipalInfo.typingUser = null;
            });
        }

        public void ShowNudge()
        {
            string nudge_text = $"{HttpUtility.UrlDecode(PrincipalInfo.displayName)} sent you a nudge!";
            Message newMessage = new Message()
            {
                MessageText = nudge_text,
                Receiver = UserInfo.displayName,
                SenderEmail = PrincipalInfo.Email,
                ReceiverEmail = UserInfo.Email,
                IsHistory = false
            };
            NullTypingUser();
            AddMessage(newMessage);
        }
    }
}
