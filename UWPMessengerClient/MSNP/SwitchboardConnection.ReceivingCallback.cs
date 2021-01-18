using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Web;
using Windows.UI.Core;

namespace UWPMessengerClient.MSNP
{
    public partial class SwitchboardConnection
    {
        private string current_response;
        private string next_response;
        public ObservableCollection<Message> MessageList { get; set; } = new ObservableCollection<Message>();
        public event EventHandler MessageReceived;

        public void ReceivingCallback(IAsyncResult asyncResult)
        {
            SwitchboardConnection switchboardConnection = (SwitchboardConnection)asyncResult.AsyncState;
            int bytes_received = switchboardConnection.SBSocket.StopReceiving(asyncResult);
            switchboardConnection.outputString = Encoding.UTF8.GetString(switchboardConnection.outputBuffer, 0, bytes_received);
            string[] responses = switchboardConnection.outputString.Split("\r\n");
            for (var i = 0; i < responses.Length; i++)
            {
                string[] res_params = responses[i].Split(" ");
                switchboardConnection.current_response = responses[i];
                if (i != responses.Length - 1)
                {
                    switchboardConnection.next_response = responses[i + 1];
                }
                try
                {
                    switchboardConnection.command_handlers[res_params[0]]();
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
                SBSocket.BeginReceiving(outputBuffer, new AsyncCallback(ReceivingCallback), switchboardConnection);
            }
        }

        protected string SeparatePayloadFromResponseWithPayload(string response, int payload_size)
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
            string[] usr_params = current_response.Split(" ");
            if (usr_params[2] != "OK")
            {
                connected = false;
            }
        }

        protected void HandleANS()
        {
            string[] ans_params = current_response.Split(" ");
            if (ans_params[2] != "OK")
            {
                connected = false;
            }
        }

        protected void HandleMSG()
        {
            string[] MSG_Responses = outputString.Split("\r\n");
            string[] MSGParams = MSG_Responses[0].Split(" ");
            string senderDisplayName = MSGParams[2];
            string length_str = MSGParams[3];
            int msg_length;
            int.TryParse(length_str, out msg_length);
            string msg_payload = SeparatePayloadFromResponseWithPayload(outputString, msg_length);
            string[] MSGPayloadParams = msg_payload.Split("\r\n");
            string[] ContentTypeParams = MSGPayloadParams[1].Split(" ");
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
                {"text/plain;", () => AddMessage(MSGPayloadParams[4], PrincipalInfo, userInfo) },
                {"text/x-msmsgscontrol", msmsgscontrolAction },
                {"text/x-msnmsgr-datacast", () => HandleDatacast(msg_payload) }
            };
            ContentTypeDictionary[ContentTypeParams[1]]();
        }

        protected void AddMessage(string message_text, UserInfo sender, UserInfo receiver)
        {
            var content = new ToastContentBuilder()
                .AddToastActivationInfo("newMessages", ToastActivationType.Foreground)
                .AddText(HttpUtility.UrlDecode(sender.displayName))
                .AddText(message_text)
                .GetToastContent();
            try
            {
                var notif = new ToastNotification(content.GetXml())
                {
                    Group = "messages"
                };
                ToastNotificationManager.CreateToastNotifier().Show(notif);
            }
            catch (ArgumentException) { }
            Message newMessage = new Message() { message_text = message_text, sender = sender.displayName, receiver = receiver.displayName, sender_email = sender.Email, receiver_email = receiver.Email, IsHistory = false };
            AddToMessageList(newMessage);
        }

        protected void AddMessage(Message message)
        {
            var content = new ToastContentBuilder()
                .AddToastActivationInfo("newMessages", ToastActivationType.Foreground)
                .AddText(HttpUtility.UrlDecode(message.sender))
                .AddText(message.message_text)
                .GetToastContent();
            try
            {
                var notif = new ToastNotification(content.GetXml())
                {
                    Group = "messages"
                };
                ToastNotificationManager.CreateToastNotifier().Show(notif);
            }
            catch (ArgumentException) { }
            AddToMessageList(message);
        }

        protected void AddToMessageList(Message message)
        {
            var task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PrincipalInfo.typingUser = null;
                MessageList.Add(message);
                if (KeepMessagingHistory)
                {
                    DatabaseAccess.AddMessageToTable(userInfo.Email, PrincipalInfo.Email, message);
                }
                MessageReceived?.Invoke(this, new EventArgs());
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

        public void ShowNudge()
        {
            string nudge_text = $"{HttpUtility.UrlDecode(PrincipalInfo.displayName)} sent you a nudge!";
            Message newMessage = new Message() { message_text = nudge_text, receiver = userInfo.displayName, sender_email = PrincipalInfo.Email, receiver_email = userInfo.Email, IsHistory = false };
            AddMessage(newMessage);
        }
    }
}
