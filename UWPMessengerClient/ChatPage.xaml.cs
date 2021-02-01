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
    public sealed partial class ChatPage : Page
    {
        private NotificationServerConnection notificationServerConnection;
        private SwitchboardConnection switchboardConnection;
        private Message MessageInContext;

        public ChatPage()
        {
            this.InitializeComponent();
            inkCanvas.InkPresenter.InputDeviceTypes =
            CoreInputDeviceTypes.Mouse |
            CoreInputDeviceTypes.Pen;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            notificationServerConnection = (NotificationServerConnection)e.Parameter;
            switchboardConnection = notificationServerConnection.SBConnection;
            notificationServerConnection.NotConnected += NotificationServerConnection_NotConnected;
            Task task = GroupMessages();
            switchboardConnection.HistoryLoaded += SwitchboardConnection_HistoryLoaded;
            switchboardConnection.MessageReceived += SwitchboardConnection_MessageReceived;
            BackButton.IsEnabled = this.Frame.CanGoBack;
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            notificationServerConnection.NotConnected -= NotificationServerConnection_NotConnected;
            switchboardConnection.HistoryLoaded -= SwitchboardConnection_HistoryLoaded;
            switchboardConnection.MessageReceived -= SwitchboardConnection_MessageReceived;
        }

        private async void NotificationServerConnection_NotConnected(object sender, EventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await ShowDialog("Error", "Connection to the server was lost: exiting...");
                this.Frame.Navigate(typeof(LoginPage));
            });
        }

        private async void SwitchboardConnection_MessageReceived(object sender, EventArgs e)
        {
            await GroupMessages();
        }

        private async void SwitchboardConnection_HistoryLoaded(object sender, EventArgs e)
        {
            await GroupMessages();
        }

        private async Task GroupMessages()
        {
            var groups = from message in switchboardConnection.MessageList
                         group message by message.IsHistory into message_group
                         select new GroupInfoList(message_group) { Key = message_group.Key };
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MessageCVS.Source = new ObservableCollection<GroupInfoList>(groups);
            });
        }

        private async Task SendMessage()
        {
            if (switchboardConnection != null && switchboardConnection.connected && messageBox.Text != "")
            {
                await switchboardConnection.SendTextMessage(messageBox.Text);
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
                await switchboardConnection.SendTypingUser();
            }
        }

        private async void nudgeButton_Click(object sender, RoutedEventArgs e)
        {
            await switchboardConnection.SendNudge();
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
                byte[] ink_bytes = memoryStream.ToArray();
                await switchboardConnection.SendInk(ink_bytes);
            }
            inkCanvas.InkPresenter.StrokeContainer.Clear();
        }

        private async Task LoadReceivedInk()
        {
            if (MessageInContext.InkBytes != null)
            {
                using (MemoryStream memoryStream = new MemoryStream(MessageInContext.InkBytes))
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
            MessageInContext = (Message)((FrameworkElement)e.OriginalSource).DataContext;
            await LoadReceivedInk();
        }

        private async void messageList_Holding(object sender, HoldingRoutedEventArgs e)
        {
            MessageInContext = (Message)((FrameworkElement)e.OriginalSource).DataContext;
            await LoadReceivedInk();
        }
    }
}
