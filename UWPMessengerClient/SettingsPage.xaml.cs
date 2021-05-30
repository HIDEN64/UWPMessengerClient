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
using Windows.Storage;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UWPMessengerClient.MSNP;

namespace UWPMessengerClient
{
    public sealed partial class SettingsPage : Page, INotifyPropertyChanged
    {
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private string serverAddress = "m1.escargot.chat";//escargot address
        private int serverPort = 1863;
        private SocketCommands testSocket;
        public event PropertyChangedEventHandler PropertyChanged;
        private NotificationServerConnection notificationServerConnection;
        private ObservableCollection<string> errors;
        private ObservableCollection<string> Errors
        {
            get => errors;
            set
            {
                errors = value;
                NotifyPropertyChanged();
            }
        }

        public SettingsPage()
        {
            this.InitializeComponent();
            SetConfigDefaultValuesIfNull();
            testSocket = new SocketCommands(serverAddress, serverPort);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            BackButton.IsEnabled = this.Frame.CanGoBack;
            SetSavedSettings();
            if (e.Parameter != null)
            {
                notificationServerConnection = (NotificationServerConnection)e.Parameter;
                Errors = notificationServerConnection.ErrorLog;
                notificationServerConnection.KeepMessagingHistoryInSwitchboard = (bool)localSettings.Values["KeepHistory"];
            }
            var task = TestServer();
            base.OnNavigatedTo(e);
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }

        private void version_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            localSettings.Values["MsnpVersion"] = version_box.SelectedItem.ToString();
            localSettings.Values["MsnpVersionIndex"] = version_box.SelectedIndex;
        }

        private void localhost_toggle_Toggled(object sender, RoutedEventArgs e)
        {
            localSettings.Values["UsingLocalhost"] = localhost_toggle.IsOn;
        }

        private void SetConfigDefaultValuesIfNull()
        {
            if (localSettings.Values["MsnpVersion"] == null)
            {
                localSettings.Values["MsnpVersion"] = "MSNP15";
            }
            if (localSettings.Values["MsnpVersionIndex"] == null)
            {
                localSettings.Values["MsnpVersionIndex"] = 0;
            }
            if (localSettings.Values["UsingLocalhost"] == null)
            {
                localSettings.Values["UsingLocalhost"] = false;
            }
            if (localSettings.Values["KeepHistory"] == null)
            {
                localSettings.Values["KeepHistory"] = true;
            }
        }

        private async Task TestServer()
        {
            server_connection_status.Text = "Testing server response time...";
            Stopwatch stopwatch = new Stopwatch();
            string status = "";
            await Task.Run(() =>
            {
                testSocket.ConnectSocket();
                testSocket.SetReceiveTimeout(25000);
                byte[] buffer = new byte[4096];
                testSocket.SendCommand("VER 1 MSNP15 CVR0\r\n");
                stopwatch.Start();
                try
                {
                    testSocket.ReceiveMessage(buffer);
                    status = "Connected to server";
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    if (e.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut)
                    {
                        status = "Could not connect to server";
                    }
                }
                stopwatch.Stop();
            });
            server_connection_status.Text = $"{status} - {stopwatch.Elapsed.TotalSeconds} seconds response time";
        }

        private void SetSavedSettings()
        {
            version_box.SelectedIndex = (int)localSettings.Values["MsnpVersionIndex"];
            localhost_toggle.IsOn = (bool)localSettings.Values["UsingLocalhost"];
            MessagingHistorySwitch.IsOn = (bool)localSettings.Values["KeepHistory"];
        }

        private async void testServerButton_Click(object sender, RoutedEventArgs e)
        {
            await TestServer();
        }

        private void MessagingHistorySwitch_Toggled(object sender, RoutedEventArgs e)
        {
            localSettings.Values["KeepHistory"] = MessagingHistorySwitch.IsOn;
            if (notificationServerConnection != null)
            {
                notificationServerConnection.KeepMessagingHistoryInSwitchboard = (bool)localSettings.Values["KeepHistory"];
            }
        }

        private async void AcceptContactButton_Click(object sender, RoutedEventArgs e)
        {
            Contact contactToAccept = (Contact)((FrameworkElement)e.OriginalSource).DataContext;
            try
            {
                await notificationServerConnection.AcceptNewContact(contactToAccept);
            }
            catch (Exception ex)
            {
                await ShowDialog("Error", ex.Message);
            }
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
}
