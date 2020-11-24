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

namespace UWPMessengerClient
{
    public sealed partial class ChatPage : Page
    {
        private SwitchboardConnection switchboardConnection;

        public ChatPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            switchboardConnection = (SwitchboardConnection)e.Parameter;
            BackButton.IsEnabled = this.Frame.CanGoBack;
            base.OnNavigatedTo(e);
        }

        private async void sendButton_Click(object sender, RoutedEventArgs e)
        {
            if (switchboardConnection != null && switchboardConnection.connected && messageBox.Text != "")
            {
                await switchboardConnection.SendMessage(messageBox.Text);
                messageBox.Text = "";
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }
    }
}
