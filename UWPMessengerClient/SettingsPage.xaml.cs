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

namespace UWPMessengerClient
{
    public sealed partial class SettingsPage : Page
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private ObservableCollection<string> errors;

        public SettingsPage()
        {
            this.InitializeComponent();
            SetConfigDefaultValuesIfNull();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            BackButton.IsEnabled = this.Frame.CanGoBack;
            SetSavedSettings();
            errors = (ObservableCollection<string>)e.Parameter;
            base.OnNavigatedTo(e);
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
        }

        private void SetSavedSettings()
        {
            version_box.SelectedIndex = (int)localSettings.Values["MSNP_Version_Index"];
            localhost_toggle.IsOn = (bool)localSettings.Values["Using_Localhost"];
        }
    }
}
