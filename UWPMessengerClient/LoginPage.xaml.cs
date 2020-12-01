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
        NotificationServerConnection notificationServerConnection;

        public LoginPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            notificationServerConnection = null;
            base.OnNavigatedTo(e);
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            enable_progress_ring();
            string email = Email_box.Text;
            string password = Password_box.Password;
            notificationServerConnection = new NotificationServerConnection(email, password);
            try
            {
                await notificationServerConnection.LoginToMessengerAsync();
            }
            catch (AggregateException ex)
            {
                for (int i = 0; i < ex.InnerExceptions.Count; i++)
                {
                    await ShowLoginErrorDialog(ex.InnerExceptions[i].Message);
                }
                disable_progress_ring();
                return;
            }
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

        public async Task ShowLoginErrorDialog(string error)
        {
            ContentDialog loginErrorDialog = new ContentDialog
            {
                Title = "Login error",
                Content = "There was an error logging in: " + error,
                CloseButtonText = "Close"
            };
            ContentDialogResult loginErrorResult = await loginErrorDialog.ShowAsync();
        }

        private async void Login_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                enable_progress_ring();
                string email = Email_box.Text;
                string password = Password_box.Password;
                notificationServerConnection = new NotificationServerConnection(email, password);
                try
                {
                    await notificationServerConnection.LoginToMessengerAsync();
                }
                catch (AggregateException ex)
                {
                    for (int i = 0; i < ex.InnerExceptions.Count; i++)
                    {
                        await ShowLoginErrorDialog(ex.InnerExceptions[i].Message);
                    }
                    disable_progress_ring();
                    return;
                }
                this.Frame.Navigate(typeof(ContactList), notificationServerConnection);
                disable_progress_ring();
            }
        }
    }
}
