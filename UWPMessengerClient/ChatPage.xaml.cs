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
using System.Collections.ObjectModel;
using System.Threading.Tasks;

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
            switchboardConnection = notificationServerConnection.switchboardConnection;
            BackButton.IsEnabled = this.Frame.CanGoBack;
            base.OnNavigatedTo(e);
        }

        private async Task SendMessage()
        {
            switch (notificationServerConnection.MSNPVersionSelected)
            {
                case "MSNP12":
                    if (notificationServerConnection.switchboardConnection != null && notificationServerConnection.switchboardConnection.connected && messageBox.Text != "")
                    {
                        await notificationServerConnection.switchboardConnection.SendMessage(messageBox.Text);
                        messageBox.Text = "";
                    }
                    break;
                case "MSNP15":
                    throw new NotImplementedException();
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
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
    }
}
