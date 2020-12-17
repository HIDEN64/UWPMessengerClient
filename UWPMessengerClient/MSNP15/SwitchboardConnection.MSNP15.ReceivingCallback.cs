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

namespace UWPMessengerClient.MSNP15
{
    public partial class SwitchboardConnection
    {
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
                    var task = ProduceTypingUser();
                }
                else
                {
                    AddMessageToList();
                }
            }
            if (switchboardConnection.outputString.Contains("OK"))
            {
                connected = true;
            }
            if (switchboardConnection.outputString.Contains("JOI") || switchboardConnection.outputString.Contains("IRO"))
            {
                principalsConnected++;
            }
            if (bytes_received > 0)
            {
                SBSocket.BeginReceiving(outputBuffer, new AsyncCallback(ReceivingCallback), switchboardConnection);
            }
        }

        public void AddMessageToList()
        {
            string messageText = outputString.Substring(outputString.LastIndexOf("\r\n") + 2);//2 counting for \r and \n
            string[] MSGParams = outputString.Split(" ");
            string senderDisplayName = MSGParams[2];
            var content = new ToastContentBuilder()
                .AddToastActivationInfo("newMessage", ToastActivationType.Foreground)
                .AddText(HttpUtility.UrlDecode(senderDisplayName))
                .AddText(messageText)
                .GetToastContent();
            try
            {
                var notif = new ToastNotification(content.GetXml());
                ToastNotificationManager.CreateToastNotifier().Show(notif);
            }
            catch (ArgumentException) { }
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PrincipalInfo.typingUser = null;
                MessageList.Add(new Message() { message_text = messageText, sender = senderDisplayName });
            });
        }

        public async Task ProduceTypingUser()
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
