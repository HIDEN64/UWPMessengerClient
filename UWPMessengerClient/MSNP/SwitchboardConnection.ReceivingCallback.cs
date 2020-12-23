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

        public void ReceivingCallback(IAsyncResult asyncResult)
        {
            SwitchboardConnection switchboardConnection = (SwitchboardConnection)asyncResult.AsyncState;
            int bytes_received = switchboardConnection.SBSocket.StopReceiving(asyncResult);
            switchboardConnection.outputString = Encoding.UTF8.GetString(switchboardConnection.outputBuffer, 0, bytes_received);
            if (switchboardConnection.outputString.StartsWith("MSG"))
            {
                if (switchboardConnection.outputString.Contains("TypingUser"))
                {
                    var task = switchboardConnection.HandleTypingUser();
                }
                else
                {
                    switchboardConnection.HandleMSG();
                }
            }
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

        public void HandleUSR()
        {
            string[] usr_params = current_response.Split(" ");
            if (usr_params[2] == "OK")
            {
                connected = true;
            }
        }

        public void HandleANS()
        {
            string[] ans_params = current_response.Split(" ");
            if (ans_params[2] == "OK")
            {
                connected = true;
            }
        }

        public void HandleMSG()
        {
            string messageText = outputString.Substring(outputString.LastIndexOf("\r\n") + 2);//2 counting for \r and \n
            string[] MSGParams = outputString.Split(" ");
            string senderDisplayName = MSGParams[2];
            var content = new ToastContentBuilder()
                .AddToastActivationInfo("newMessages", ToastActivationType.Foreground)
                .AddText(HttpUtility.UrlDecode(senderDisplayName))
                .AddText(messageText)
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
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PrincipalInfo.typingUser = null;
                MessageList.Add(new Message() { message_text = messageText, sender = senderDisplayName });
            });
        }

        public async Task HandleTypingUser()
        {
            Windows.Foundation.IAsyncAction set_task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PrincipalInfo.typingUser = $"{HttpUtility.UrlDecode(PrincipalInfo.displayName)} is typing...";
            });
            await Task.Delay(6000);
            Windows.Foundation.IAsyncAction null_task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PrincipalInfo.typingUser = null;
            });
        }
    }
}
