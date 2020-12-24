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

namespace UWPMessengerClient
{
    public sealed partial class ChatPage : Page
    {
        private MSNP.NotificationServerConnection notificationServerConnection;
        private MSNP.SwitchboardConnection switchboardConnection;

        public ChatPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            notificationServerConnection = (MSNP.NotificationServerConnection)e.Parameter;
            switchboardConnection = notificationServerConnection.SBConnection;
            BackButton.IsEnabled = this.Frame.CanGoBack;
            base.OnNavigatedTo(e);
        }

        private async Task SendMessage()
        {
            if (switchboardConnection != null && switchboardConnection.connected && messageBox.Text != "")
            {
                await switchboardConnection.SendMessage(messageBox.Text);
                messageBox.Text = "";
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
    }
}
