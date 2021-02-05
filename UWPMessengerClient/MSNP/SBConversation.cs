using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Windows.UI.Core;
using Windows.UI.Notifications;

namespace UWPMessengerClient.MSNP
{
    public class SBConversation : INotifyPropertyChanged
    {
        private NotificationServerConnection notificationServerConnection;
        private SwitchboardConnection switchboardConnection;
        public UserInfo _UserInfo = new UserInfo();
        public UserInfo _ContactInfo = new UserInfo();
        public string ConversationID
        { get; private set; }
        public event EventHandler MessageListUpdated;
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<Message> _Messages;
        public ObservableCollection<Message> Messages
        {
            get => _Messages;
            private set
            {
                _Messages = value;
                MessageListUpdated?.Invoke(this, new EventArgs());
            }
        }
        public UserInfo UserInfo
        {
            get => _UserInfo;
            set
            {
                _UserInfo = value;
                NotifyPropertyChanged();
            }
        }
        public UserInfo ContactInfo
        {
            get => _ContactInfo;
            set
            {
                _ContactInfo = value;
                NotifyPropertyChanged();
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        public SBConversation(NotificationServerConnection notificationConnection)
        {
            notificationServerConnection = notificationConnection;
            UserInfo = notificationServerConnection.userInfo;
            notificationServerConnection.SwitchboardCreated += NotificationServerConnection_SwitchboardCreated;
        }

        public SBConversation(NotificationServerConnection notificationConnection, string conversation_id)
        {
            notificationServerConnection = notificationConnection;
            UserInfo = notificationServerConnection.userInfo;
            notificationServerConnection.SwitchboardCreated += NotificationServerConnection_SwitchboardCreated;
            ConversationID = conversation_id;
        }

        public async Task SendTypingUser()
        {
            await switchboardConnection.SendTypingUser();
        }

        public async Task SendTextMessage(string message_text)
        {
            await switchboardConnection.SendTextMessage(message_text);
        }

        public async Task SendNudge()
        {
            await switchboardConnection.SendNudge();
        }

        public async Task SendInk(byte[] InkBytes)
        {
            await switchboardConnection.SendInk(InkBytes);
        }

        private void AssignSwitchboard(SwitchboardConnection switchboard)
        {
            switchboardConnection = switchboard;
            ContactInfo = switchboardConnection.PrincipalInfo;
            switchboardConnection.HistoryLoaded += SwitchboardConnection_HistoryLoaded;
            switchboardConnection.NewMessage += SwitchboardConnection_NewMessage;
            switchboardConnection.MessageReceived += SwitchboardConnection_MessageReceived;
        }

        private void SwitchboardConnection_MessageReceived(object sender, MessageEventArgs e)
        {
            SendMessageToast(e.message.message_text, e.message.sender);
        }

        private void SwitchboardConnection_NewMessage(object sender, EventArgs e)
        {
            Messages = switchboardConnection.MessageList;
        }

        private void SwitchboardConnection_HistoryLoaded(object sender, EventArgs e)
        {
            Messages = switchboardConnection.MessageList;
        }

        private void NotificationServerConnection_SwitchboardCreated(object sender, SwitchboardEventArgs e)
        {
            if (switchboardConnection is null)
            {
                AssignSwitchboard(e.switchboard);
            }
            else if (ContactInfo.Email == e.switchboard.PrincipalInfo.Email)
            {
                ExitFromConversation();
                AssignSwitchboard(e.switchboard);
            }
        }

        private void SendMessageToast(string message_text, string message_sender)
        {
            var content = new ToastContentBuilder()
                .AddToastActivationInfo(new QueryString()
                {
                    {"action", "newMessage" },
                    {"conversationID", ConversationID }
                }.ToString(), ToastActivationType.Foreground)
                .AddText(HttpUtility.UrlDecode(message_sender))
                .AddText(message_text)
                .AddInputTextBox("ReplyBox", "Type your reply")
                .AddButton("Reply", ToastActivationType.Background, new QueryString()
                {
                    {"action", "ReplyMessage" },
                    {"conversationID", ConversationID }
                }.ToString())
                .AddButton("Dismiss all", ToastActivationType.Background, new QueryString()
                {
                    {"action", "DismissMessages" }
                }.ToString())
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
        }

        private void ExitFromConversation()
        {
            switchboardConnection.Exit();
            switchboardConnection.HistoryLoaded -= SwitchboardConnection_HistoryLoaded;
            switchboardConnection.NewMessage -= SwitchboardConnection_NewMessage;
            switchboardConnection.MessageReceived -= SwitchboardConnection_MessageReceived;
            notificationServerConnection.SwitchboardCreated -= NotificationServerConnection_SwitchboardCreated;
        }

        ~SBConversation()
        {
            ExitFromConversation();
        }
    }
}
