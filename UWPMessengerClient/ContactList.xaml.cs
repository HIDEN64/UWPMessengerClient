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
        private NotificationServerConnection notificationServerConnection;
        private ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
        private Contact contactInContext;
        public event PropertyChangedEventHandler PropertyChanged;

        private NotificationServerConnection NotificationServerConnection
        {
            get => notificationServerConnection;
            set
            {
                notificationServerConnection = value;
                NotifyPropertyChanged();
            }
        }

        public ContactList()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            NotificationServerConnection = (NotificationServerConnection)e.Parameter;
            NotificationServerConnection.NotConnected += NotificationServerConnection_NotConnected;
            string status = NotificationServerConnection.UserPresenceStatus;
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
            if (roamingSettings.Values[$"{NotificationServerConnection.UserInfo.Email}_PersonalMessage"] != null)
            {
                _ = NotificationServerConnection.SendUserPersonalMessage((string)roamingSettings.Values[$"{NotificationServerConnection.UserInfo.Email}_PersonalMessage"]);
            }
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            NotificationServerConnection.NotConnected -= NotificationServerConnection_NotConnected;
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
            if (NotificationServerConnection != null)
            {
                string selectedStatus = e.AddedItems[0].ToString();
                switch (selectedStatus)
                {
                    case "Available":
                        await NotificationServerConnection.ChangePresence(PresenceStatuses.Available);
                        break;
                    case "Busy":
                        await NotificationServerConnection.ChangePresence(PresenceStatuses.Busy);
                        break;
                    case "Away":
                        await NotificationServerConnection.ChangePresence(PresenceStatuses.Away);
                        break;
                    case "Invisible":
                        await NotificationServerConnection.ChangePresence(PresenceStatuses.Hidden);
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
                    string conversationId = await NotificationServerConnection.StartChat(contactInContext);
                    Frame.Navigate(typeof(ChatPage), new ChatPageNavigationParameters()
                    {
                        NotificationServerConnection = NotificationServerConnection,
                        SbConversationId = conversationId
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
            NotificationServerConnection.Exit();
            this.Frame.Navigate(typeof(LoginPage));
        }

        private async void start_chat_button_Click(object sender, RoutedEventArgs e)
        {
            contactInContext = (Contact)((FrameworkElement)e.OriginalSource).DataContext;
            await StartChat();
        }

        private async void ChangeUserDisplayNameConfirmationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await NotificationServerConnection.ChangeUserDisplayName(ChangeUserDisplayNameTextBox.Text);
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
                await NotificationServerConnection.AddNewContact(contactEmailBox.Text, contactDisplayNameBox.Text);
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
            this.Frame.Navigate(typeof(SettingsPage), NotificationServerConnection);
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
                await NotificationServerConnection.SendUserPersonalMessage(ChangeUserPersonalMessageTextBox.Text);
                ChangeUserDisplayNameTextBox.Text = "";
                PersonalMessageErrors.Text = "";
                personalMessageFlyout.Hide();
                roamingSettings.Values[$"{NotificationServerConnection.UserInfo.Email}_PersonalMessage"] = ChangeUserPersonalMessageTextBox.Text;
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
            contactInContext = (Contact)((FrameworkElement)e.OriginalSource).DataContext;
            if (contactInContext != null)
            {
                if (contactInContext.OnBlock)
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
            contactInContext = (Contact)((FrameworkElement)e.OriginalSource).DataContext;
            await StartChat();
        }

        private void contactListView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            ListView contactListView = (ListView)sender;
            ContactMenuFlyout.ShowAt(contactListView, e.GetPosition(contactListView));
            contactInContext = (Contact)((FrameworkElement)e.OriginalSource).DataContext;
            if (contactInContext != null)
            {
                if (contactInContext.OnBlock)
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
                await NotificationServerConnection.RemoveContact(contactInContext);
            }
            catch (Exception ex)
            {
                await ShowDialog("Error", ex.Message);
            }
            contactInContext = null;
        }

        private async void BlockItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await NotificationServerConnection.BlockContact(contactInContext);
            }
            catch (Exception ex)
            {
                await ShowDialog("Error", ex.Message);
            }
            contactInContext = null;
        }

        private async void UnblockItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await NotificationServerConnection.UnblockContact(contactInContext);
            }
            catch (Exception ex)
            {
                await ShowDialog("Error", ex.Message);
            }
            contactInContext = null;
        }
    }
}
