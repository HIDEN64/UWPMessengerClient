using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using UWPMessengerClient.MSNP;
using Windows.UI.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage.Streams;

namespace UWPMessengerClient
{
    public sealed partial class ChatPage : Page, INotifyPropertyChanged
    {
        private NotificationServerConnection notificationServerConnection;
        private SBConversation conversation;
        private Message messageInContext;
        public string ConversationId { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private NotificationServerConnection NotificationServerConnection
        {
            get => notificationServerConnection;
            set
            {
                notificationServerConnection = value;
                NotifyPropertyChanged();
            }
        }
        private SBConversation Conversation
        {
            get => conversation;
            set
            {
                conversation = value;
                NotifyPropertyChanged();
            }
        }

        public ChatPage()
        {
            this.InitializeComponent();
            inkCanvas.InkPresenter.InputDeviceTypes =
            CoreInputDeviceTypes.Mouse |
            CoreInputDeviceTypes.Pen;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            ChatPageNavigationParameters navigationParameters = (ChatPageNavigationParameters)e.Parameter;
            NotificationServerConnection = navigationParameters.NotificationServerConnection;
            ConversationId = navigationParameters.SbConversationId;
            Conversation = NotificationServerConnection.ReturnConversationFromConversationId(ConversationId);
            NotificationServerConnection.NotConnected += NotificationServerConnection_NotConnected;
            Conversation.MessageListUpdated += Conversation_MessageListUpdated;
            BackButton.IsEnabled = Frame.CanGoBack;
            await GroupMessages();
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            NotificationServerConnection.NotConnected -= NotificationServerConnection_NotConnected;
            Conversation.MessageListUpdated -= Conversation_MessageListUpdated;
            base.OnNavigatedFrom(e);
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        private async void NotificationServerConnection_NotConnected(object sender, EventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await ShowDialog("Error", "Connection to the server was lost: exiting...");
                this.Frame.Navigate(typeof(LoginPage));
            });
        }

        private async void Conversation_MessageListUpdated(object sender, EventArgs e)
        {
            await GroupMessages();
        }

        private async Task GroupMessages()
        {
            if (Conversation.Messages is null) { return; }
            var groups = from message in Conversation.Messages
                         group message by message.IsHistory into message_group
                         select new GroupInfoList(message_group) { Key = message_group.Key };
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MessageCVS.Source = new ObservableCollection<GroupInfoList>(groups);
            });
        }

        private async Task SendMessage()
        {
            if (messageBox.Text != "")
            {
                await Conversation.SendTextMessage(messageBox.Text);
                messageBox.Text = "";
                await GroupMessages();
            }
        }

        private async void sendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }

        private async void messageBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                await SendMessage();
            }
        }

        private async void messageBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (messageBox.Text != "")
            {
                await Conversation.SendTypingUser();
            }
        }

        private async void nudgeButton_Click(object sender, RoutedEventArgs e)
        {
            await Conversation.SendNudge();
        }

        public async Task ShowDialog(string title, string message)
        {
            ContentDialog Dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Close"
            };
            ContentDialogResult DialogResult = await Dialog.ShowAsync();
        }

        private async void SendInkButton_Click(object sender, RoutedEventArgs e)
        {
            using(MemoryStream memoryStream = new MemoryStream())
            {
                using(IRandomAccessStream stream = memoryStream.AsRandomAccessStream())
                {
                    await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(stream);
                }
                byte[] inkBytes = memoryStream.ToArray();
                await Conversation.SendInk(inkBytes);
            }
            inkCanvas.InkPresenter.StrokeContainer.Clear();
        }

        private async Task LoadReceivedInk()
        {
            if (messageInContext.InkBytes != null)
            {
                using (MemoryStream memoryStream = new MemoryStream(messageInContext.InkBytes))
                {
                    using (IRandomAccessStream stream = memoryStream.AsRandomAccessStream())
                    {
                        await ReceivedInkCanvas.InkPresenter.StrokeContainer.LoadAsync(stream);
                    }
                }
                MessagePivot.SelectedIndex = 2;//received ink index
            }
        }

        private async void messageList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            messageInContext = (Message)((FrameworkElement)e.OriginalSource).DataContext;
            await LoadReceivedInk();
        }

        private async void messageList_Holding(object sender, HoldingRoutedEventArgs e)
        {
            messageInContext = (Message)((FrameworkElement)e.OriginalSource).DataContext;
            await LoadReceivedInk();
        }
    }
}
