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
using Windows.Storage;
using UWPMessengerClient.MSNP;

namespace UWPMessengerClient
{
    public sealed partial class ContactList : Page
    {
        private NotificationServerConnection notificationServerConnection;
        ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

        public ContactList()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            notificationServerConnection = (NotificationServerConnection)e.Parameter;
            string status = notificationServerConnection.UserPresenceStatus;
            string fullStatus = null;
            switch (status)
            {
                case PresenceStatuses.Available:
                    fullStatus = "Available";
                    break;
                case PresenceStatuses.Busy:
                    fullStatus = "Busy";
                    break;
                case PresenceStatuses.Away:
                    fullStatus = "Away";
                    break;
                case PresenceStatuses.Hidden:
                    fullStatus = "Invisible";
                    break;
            }
            Presence.SelectedItem = fullStatus;
            if (roamingSettings.Values[$"{notificationServerConnection.userInfo.Email}_PersonalMessage"] != null)
            {
                _ = notificationServerConnection.SendUserPersonalMessage((string)roamingSettings.Values[$"{notificationServerConnection.userInfo.Email}_PersonalMessage"]);
            }
            base.OnNavigatedTo(e);
        }

        private async void Presence_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (notificationServerConnection != null)
            {
                string selectedStatus = e.AddedItems[0].ToString();
                switch (selectedStatus)
                {
                    case "Available":
                        await notificationServerConnection.ChangePresence(PresenceStatuses.Available);
                        break;
                    case "Busy":
                        await notificationServerConnection.ChangePresence(PresenceStatuses.Busy);
                        break;
                    case "Away":
                        await notificationServerConnection.ChangePresence(PresenceStatuses.Away);
                        break;
                    case "Invisible":
                        await notificationServerConnection.ChangePresence(PresenceStatuses.Hidden);
                        break;
                }
            }
        }

        private async Task StartChat()
        {
            if (contactListView.SelectedIndex >= 0)
            {
                try
                {
                    if (notificationServerConnection.ContactIndexToChat != contactListView.SelectedIndex || notificationServerConnection.SBConnection == null)
                    {
                        notificationServerConnection.ContactIndexToChat = contactListView.SelectedIndex;
                        await notificationServerConnection.InitiateSB();
                    }
                    this.Frame.Navigate(typeof(ChatPage), notificationServerConnection);
                }
                catch (Exception e)
                {
                    await ShowErrorDialog(e.Message);
                }
            }
            else
            {
                return;
            }
        }

        private async void start_chat_button_Click(object sender, RoutedEventArgs e)
        {
            await StartChat();
        }

        private async void ChangeUserDisplayNameConfirmationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await notificationServerConnection.ChangeUserDisplayName(ChangeUserDisplayNameTextBox.Text);
                ChangeUserDisplayNameTextBox.Text = "";
                DisplayNameErrors.Text = "";
                ChangeFlyout.Hide();
            }
            catch (Exception ex)
            {
                DisplayNameErrors.Text = "There was an error:\n" + ex.Message;
            }
        }

        private void TextBlock_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private async void StackPanel_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            await StartChat();
        }

        private async void addContactButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await notificationServerConnection.AddContact(contactEmailBox.Text, contactDisplayNameBox.Text);
                contactDisplayNameBox.Text = "";
                contactEmailBox.Text = "";
                AddContactErrors.Text = "";
                addContactAppBarButton.Flyout.Hide();
            }
            catch (Exception ex)
            {
                AddContactErrors.Text = "There was an error:\n" + ex.Message;
            }
        }

        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            notificationServerConnection.Exit();
            notificationServerConnection = null;
            this.Frame.Navigate(typeof(LoginPage));
        }

        private void addContactAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            addContactAppBarButton.Flyout.ShowAt((FrameworkElement)sender);
        }

        private async void removeContactAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (contactListView.SelectedIndex >= 0)
            {
                await notificationServerConnection.RemoveContact(notificationServerConnection.contacts_in_forward_list[contactListView.SelectedIndex]);
            }
        }

        private void settings_button_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage), notificationServerConnection);
        }

        private void addContactFlyout_Closed(object sender, object e)
        {
            AddContactErrors.Text = "";
        }

        private void ChangeFlyout_Closed(object sender, object e)
        {
            DisplayNameErrors.Text = "";
        }

        private void personalMessageFlyout_Closed(object sender, object e)
        {
            PersonalMessageErrors.Text = "";
        }

        private async void ChangeUserPersonalConfirmationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await notificationServerConnection.SendUserPersonalMessage(ChangeUserPersonalMessageTextBox.Text);
                ChangeUserDisplayNameTextBox.Text = "";
                PersonalMessageErrors.Text = "";
                personalMessageFlyout.Hide();
                roamingSettings.Values[$"{notificationServerConnection.userInfo.Email}_PersonalMessage"] = ChangeUserPersonalMessageTextBox.Text;
            }
            catch (Exception ex)
            {
                PersonalMessageErrors.Text = "There was an error:\n" + ex.Message;
            }
        }

        private void UserPersonalMessage_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        public async Task ShowErrorDialog(string error)
        {
            ContentDialog ErrorDialog = new ContentDialog
            {
                Title = "Error",
                Content = "There was an error, please try again. Error: " + error,
                CloseButtonText = "Close"
            };
            ContentDialogResult ErrorResult = await ErrorDialog.ShowAsync();
        }
    }
}
