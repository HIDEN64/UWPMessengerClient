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
using System.Net.Sockets;
using Windows.Storage;
using UWPMessengerClient.MSNP;

namespace UWPMessengerClient
{
    public sealed partial class LoginPage : Page
    {
        NotificationServerConnection notificationServerConnection;
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public LoginPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            notificationServerConnection = null;
            DisableProgressRingAndShowButtons();
            base.OnNavigatedTo(e);
        }

        private async Task StartLogin()
        {
            EnableProgressRingAndHideButtons();
            string email = Email_box.Text;
            string password = Password_box.Password;
            if (email == "" || password == "")
            {
                await ShowLoginErrorDialog("Please type login and password");
                DisableProgressRingAndShowButtons();
                return;
            }
            string selected_version = "MSNP15";
            if (localSettings.Values["MSNP_Version"] != null)
            {
                selected_version = localSettings.Values["MSNP_Version"].ToString();
            }
            bool using_localhost = false;
            if (localSettings.Values["Using_Localhost"] != null)
            {
                using_localhost = (bool)localSettings.Values["Using_Localhost"];
            }
            string initial_status = PresenceStatuses.Available;
            switch (InitialStatusBox.SelectedItem.ToString())
            {
                case "Available":
                    initial_status = PresenceStatuses.Available;
                    break;
                case "Busy":
                    initial_status = PresenceStatuses.Busy;
                    break;
                case "Away":
                    initial_status = PresenceStatuses.Away;
                    break;
                case "Invisible":
                    initial_status = PresenceStatuses.Hidden;
                    break;
            }
            notificationServerConnection = new NotificationServerConnection(email, password, using_localhost, selected_version, initial_status);
            if (localSettings.Values["KeepHistory"] != null)
            {
                notificationServerConnection.KeepMessagingHistoryInSwitchboard = (bool)localSettings.Values["KeepHistory"];
            }
            try
            {
                await notificationServerConnection.LoginToMessengerAsync();
            }
            catch (AggregateException ae)
            {
                for (int i = 0; i < ae.InnerExceptions.Count; i++)
                {
                    await ShowLoginErrorDialog(ae.InnerExceptions[i].Message);
                }
                DisableProgressRingAndShowButtons();
                return;
            }
            catch (NullReferenceException)
            {
                await ShowLoginErrorDialog("Incorrect email or password");
                DisableProgressRingAndShowButtons();
                return;
            }
            catch (SocketException se)
            {
                await ShowLoginErrorDialog("Server connection error, " + se.Message);
                DisableProgressRingAndShowButtons();
                return;
            }
            catch (Exception e)
            {
                await ShowLoginErrorDialog(e.Message);
                DisableProgressRingAndShowButtons();
                return;
            }
            this.Frame.Navigate(typeof(ContactList), notificationServerConnection);
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            await StartLogin();
        }

        private void EnableProgressRingAndHideButtons()
        {
            loginProgress.IsActive = true;
            loginProgress.Visibility = Visibility.Visible;
            Login.Visibility = Visibility.Collapsed;
            SettingsButton.Visibility = Visibility.Collapsed;
        }

        private void DisableProgressRingAndShowButtons()
        {
            loginProgress.Visibility = Visibility.Collapsed;
            loginProgress.IsActive = false;
            Login.Visibility = Visibility.Visible;
            SettingsButton.Visibility = Visibility.Visible;
        }

        public async Task ShowLoginErrorDialog(string error)
        {
            ContentDialog loginErrorDialog = new ContentDialog
            {
                Title = "Login error",
                Content = "There was an error logging in, please try again. Error: " + error,
                CloseButtonText = "Close"
            };
            ContentDialogResult loginErrorResult = await loginErrorDialog.ShowAsync();
        }

        private async void Login_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                await StartLogin();
            }
        }

        private void settingsItem_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsButton.Flyout.ShowAt((FrameworkElement)sender);
        }
    }
}
