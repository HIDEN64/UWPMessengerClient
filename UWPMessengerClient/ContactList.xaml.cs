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
using Windows.UI.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UWPMessengerClient
{
    public sealed partial class ContactList : Page, INotifyPropertyChanged
    {
        private NotificationServerConnection _notificationServerConnection;
        private ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
        private Contact ContactInContext;
        public event PropertyChangedEventHandler PropertyChanged;

        private NotificationServerConnection notificationServerConnection
        {
            get => _notificationServerConnection;
            set
            {
                _notificationServerConnection = value;
                NotifyPropertyChanged();
            }
        }

        public ContactList()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            notificationServerConnection = (NotificationServerConnection)e.Parameter;
            notificationServerConnection.NotConnected += NotificationServerConnection_NotConnected;
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

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            notificationServerConnection.NotConnected -= NotificationServerConnection_NotConnected;
            base.OnNavigatedFrom(e);
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        private async void NotificationServerConnection_NotConnected(object sender, EventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await ShowDialog("Error", "Connection to the server was lost: exiting...");
                this.Frame.Navigate(typeof(LoginPage));
            });
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
                    string conversationID = await notificationServerConnection.StartChat(ContactInContext);
                    Frame.Navigate(typeof(ChatPage), new ChatPageNavigationParams()
                    {
                        notificationServerConnection = notificationServerConnection,
                        SBConversationID = conversationID
                    });
                }
                catch (Exception e)
                {
                    await ShowDialog("Error", $"There was an error, please try again. Error: {e.Message}");
                }
            }
            else
            {
                return;
            }
        }

        private void Exit()
        {
            notificationServerConnection.Exit();
            this.Frame.Navigate(typeof(LoginPage));
        }

        private async void start_chat_button_Click(object sender, RoutedEventArgs e)
        {
            ContactInContext = (Contact)((FrameworkElement)e.OriginalSource).DataContext;
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

        private async void addContactButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await notificationServerConnection.AddNewContact(contactEmailBox.Text, contactDisplayNameBox.Text);
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
            Exit();
        }

        private void addContactAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            addContactAppBarButton.Flyout.ShowAt((FrameworkElement)sender);
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

        private void contactListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ListView contactListView = (ListView)sender;
            ContactMenuFlyout.ShowAt(contactListView, e.GetPosition(contactListView));
            ContactInContext = (Contact)((FrameworkElement)e.OriginalSource).DataContext;
            if (ContactInContext != null)
            {
                if (ContactInContext.onBlock)
                {
                    UnblockItem.Visibility = Visibility.Visible;
                    BlockItem.Visibility = Visibility.Collapsed;
                }
                else
                {
                    BlockItem.Visibility = Visibility.Visible;
                    UnblockItem.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void contactListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ContactInContext = (Contact)((FrameworkElement)e.OriginalSource).DataContext;
            await StartChat();
        }

        private void contactListView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            ListView contactListView = (ListView)sender;
            ContactMenuFlyout.ShowAt(contactListView, e.GetPosition(contactListView));
            ContactInContext = (Contact)((FrameworkElement)e.OriginalSource).DataContext;
            if (ContactInContext != null)
            {
                if (ContactInContext.onBlock)
                {
                    UnblockItem.Visibility = Visibility.Visible;
                    BlockItem.Visibility = Visibility.Collapsed;
                }
                else
                {
                    BlockItem.Visibility = Visibility.Visible;
                    UnblockItem.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await notificationServerConnection.RemoveContact(ContactInContext);
            }
            catch (Exception ex)
            {
                await ShowDialog("Error", ex.Message);
            }
            ContactInContext = null;
        }

        private async void BlockItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await notificationServerConnection.BlockContact(ContactInContext);
            }
            catch (Exception ex)
            {
                await ShowDialog("Error", ex.Message);
            }
            ContactInContext = null;
        }

        private async void UnblockItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await notificationServerConnection.UnblockContact(ContactInContext);
            }
            catch (Exception ex)
            {
                await ShowDialog("Error", ex.Message);
            }
            ContactInContext = null;
        }
    }
}
