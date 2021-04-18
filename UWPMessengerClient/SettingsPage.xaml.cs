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
        private string server_address = "m1.escargot.chat";//escargot address
        private int server_port = 1863;
        private SocketCommands TestSocket;
        public event PropertyChangedEventHandler PropertyChanged;
        private NotificationServerConnection notificationServerConnection;
        private ObservableCollection<string> _errors;
        private ObservableCollection<string> errors
        {
            get => _errors;
            set
            {
                _errors = value;
                NotifyPropertyChanged();
            }
        }

        public SettingsPage()
        {
            this.InitializeComponent();
            SetConfigDefaultValuesIfNull();
            TestSocket = new SocketCommands(server_address, server_port);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            BackButton.IsEnabled = this.Frame.CanGoBack;
            SetSavedSettings();
            if (e.Parameter != null)
            {
                notificationServerConnection = (NotificationServerConnection)e.Parameter;
                errors = notificationServerConnection.ErrorLog;
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
            localSettings.Values["MSNP_Version"] = version_box.SelectedItem.ToString();
            localSettings.Values["MSNP_Version_Index"] = version_box.SelectedIndex;
        }

        private void localhost_toggle_Toggled(object sender, RoutedEventArgs e)
        {
            localSettings.Values["Using_Localhost"] = localhost_toggle.IsOn;
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
                TestSocket.ConnectSocket();
                TestSocket.SetReceiveTimeout(25000);
                byte[] buffer = new byte[4096];
                TestSocket.SendCommand("VER 1 MSNP15 CVR0\r\n");
                stopwatch.Start();
                try
                {
                    TestSocket.ReceiveMessage(buffer);
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
            version_box.SelectedIndex = (int)localSettings.Values["MSNP_Version_Index"];
            localhost_toggle.IsOn = (bool)localSettings.Values["Using_Localhost"];
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
