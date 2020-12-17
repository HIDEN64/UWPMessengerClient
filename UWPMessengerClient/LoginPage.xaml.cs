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

namespace UWPMessengerClient
{
    public sealed partial class LoginPage : Page
    {
        MSNP12.NotificationServerConnection MSNP12notificationServerConnection;
        MSNP15.NotificationServerConnection MSNP15notificationServerConnection;
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public LoginPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MSNP12notificationServerConnection = null;
            MSNP15notificationServerConnection = null;
            DisableProgressRingAndShowLogin();
            base.OnNavigatedTo(e);
        }

        private async Task StartLogin()
        {
            EnableProgressRingAndHideLogin();
            SetConfigDefaultValuesIfNull();
            string email = Email_box.Text;
            string password = Password_box.Password;
            if (email == "" || password == "")
            {
                await ShowLoginErrorDialog("Please type login and password");
                DisableProgressRingAndShowLogin();
                return;
            }
            string selected_version = localSettings.Values["MSNP_Version"].ToString();
            bool using_localhost = (bool)localSettings.Values["Using_Localhost"];
            switch (selected_version)
            {
                case "MSNP12":
                    MSNP12notificationServerConnection = new MSNP12.NotificationServerConnection(email, password, using_localhost);
                    try
                    {
                        await MSNP12notificationServerConnection.LoginToMessengerAsync();
                    }
                    catch (AggregateException ae)
                    {
                        for (int i = 0; i < ae.InnerExceptions.Count; i++)
                        {
                            await ShowLoginErrorDialog(ae.InnerExceptions[i].Message);
                        }
                        DisableProgressRingAndShowLogin();
                        return;
                    }
                    catch (SocketException se)
                    {
                        await ShowLoginErrorDialog("Server connection error, code: " + se.NativeErrorCode);
                        DisableProgressRingAndShowLogin();
                        return;
                    }
                    this.Frame.Navigate(typeof(MSNP12.ContactList), MSNP12notificationServerConnection);
                    break;
                case "MSNP15":
                    MSNP15notificationServerConnection = new MSNP15.NotificationServerConnection(email, password, using_localhost);
                    try
                    {
                        await MSNP15notificationServerConnection.LoginToMessengerAsync();
                    }
                    catch (AggregateException ae)
                    {
                        for (int i = 0; i < ae.InnerExceptions.Count; i++)
                        {
                            await ShowLoginErrorDialog(ae.InnerExceptions[i].Message);
                        }
                        DisableProgressRingAndShowLogin();
                        return;
                    }
                    catch (NullReferenceException)
                    {
                        await ShowLoginErrorDialog("Incorrect email or password");
                        DisableProgressRingAndShowLogin();
                        return;
                    }
                    catch (SocketException se)
                    {
                        await ShowLoginErrorDialog("Server connection error, code: " + se.NativeErrorCode);
                        DisableProgressRingAndShowLogin();
                        return;
                    }
                    this.Frame.Navigate(typeof(MSNP15.ContactList), MSNP15notificationServerConnection);
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
        }

        private void SetConfigDefaultValuesIfNull()
        {
            if (localSettings.Values["MSNP_Version"] == null)
            {
                localSettings.Values["MSNP_Version"] = "MSNP15";
            }
            if (localSettings.Values["MSNP_Version_Index"] == null)
            {
                localSettings.Values["MSNP_Version_Index"] = 0;
            }
            if (localSettings.Values["Using_Localhost"] == null)
            {
                localSettings.Values["Using_Localhost"] = false;
            }
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            await StartLogin();
        }

        private void EnableProgressRingAndHideLogin()
        {
            loginProgress.IsActive = true;
            loginProgress.Visibility = Visibility.Visible;
            Login.Visibility = Visibility.Collapsed;
        }

        private void DisableProgressRingAndShowLogin()
        {
            loginProgress.Visibility = Visibility.Collapsed;
            loginProgress.IsActive = false;
            Login.Visibility = Visibility.Visible;
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
