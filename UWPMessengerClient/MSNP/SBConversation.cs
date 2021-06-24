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
        private UserInfo userInfo = new UserInfo();
        private UserInfo contactInfo = new UserInfo();
        public string ConversationId
        { get; private set; }
        public event EventHandler MessageListUpdated;
        public event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<Message> messages;
        public ObservableCollection<Message> Messages
        {
            get => messages;
            private set
            {
                messages = value;
                MessageListUpdated?.Invoke(this, new EventArgs());
            }
        }
        public UserInfo UserInfo
        {
            get => userInfo;
            set
            {
                userInfo = value;
                NotifyPropertyChanged();
            }
        }
        public UserInfo ContactInfo
        {
            get => contactInfo;
            set
            {
                contactInfo = value;
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
            UserInfo = notificationServerConnection.UserInfo;
            notificationServerConnection.SwitchboardCreated += NotificationServerConnection_SwitchboardCreated;
        }

        public SBConversation(NotificationServerConnection notificationConnection, string conversationId)
        {
            notificationServerConnection = notificationConnection;
            UserInfo = notificationServerConnection.UserInfo;
            notificationServerConnection.SwitchboardCreated += NotificationServerConnection_SwitchboardCreated;
            ConversationId = conversationId;
        }

        public async Task SendTypingUser()
        {
            await switchboardConnection.SendTypingUser();
        }

        public async Task SendTextMessage(string messageText)
        {
            await switchboardConnection.SendTextMessage(messageText);
        }

        public async Task SendNudge()
        {
            await switchboardConnection.SendNudge();
        }

        public async Task SendInk(byte[] inkBytes)
        {
            await switchboardConnection.SendInk(inkBytes);
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
            SendMessageToast(e.message.MessageText, e.message.Sender);
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

        private void SendMessageToast(string messageText, string messageSender)
        {
            var content = new ToastContentBuilder()
                .AddToastActivationInfo(new QueryString()
                {
                    {"action", "newMessage" },
                    {"conversationId", ConversationId }
                }.ToString(), ToastActivationType.Foreground)
                .AddText(HttpUtility.UrlDecode(messageSender))
                .AddText(messageText)
                .AddInputTextBox("ReplyBox", "Type your reply")
                .AddButton("Reply", ToastActivationType.Background, new QueryString()
                {
                    {"action", "ReplyMessage" },
                    {"conversationId", ConversationId }
                }.ToString())
                .AddButton("Dismiss all", ToastActivationType.Background, new QueryString()
                {
                    {"action", "DismissMessages" }
                }.ToString())
                .GetToastContent();
            try
            {
                var notification = new ToastNotification(content.GetXml())
                {
                    Group = "messages"
                };
                ToastNotificationManager.CreateToastNotifier().Show(notification);
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
