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
using System.Net.Http;

namespace UWPMessengerClient
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            enable_progress_ring();
            string email = Email_box.Text;
            string password = Password_box.Password;
            NotificationServerConnection notificationServerConnection = new NotificationServerConnection(email, password);
            await notificationServerConnection.LoginToMessengerAsync();
            this.Frame.Navigate(typeof(ContactList), notificationServerConnection);
            disable_progress_ring();
        }

        private void enable_progress_ring()
        {
            loginProgress.IsActive = true;
            loginProgress.Visibility = Visibility.Visible;
        }

        private void disable_progress_ring()
        {
            loginProgress.Visibility = Visibility.Collapsed;
            loginProgress.IsActive = false;
        }
    }
}
