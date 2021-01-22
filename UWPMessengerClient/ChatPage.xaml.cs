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

namespace UWPMessengerClient
{
    public sealed partial class ChatPage : Page
    {
        private NotificationServerConnection notificationServerConnection;
        private SwitchboardConnection switchboardConnection;

        public ChatPage()
        {
            this.InitializeComponent();
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
                await switchboardConnection.SendMessage(messageBox.Text);
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
    }

    public class GroupInfoList : List<object>, INotifyPropertyChanged
    {
        public GroupInfoList(IEnumerable<object> items) : base(items) { }
        public event PropertyChangedEventHandler PropertyChanged;
        private object _Key;
        public object Key
        {
            get => _Key;
            set
            {
                _Key = value;
                NotifyPropertyChanged();
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
