using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace UWPMessengerClient
{
    public class NotificationServerConnection
    {
        public string MSNPVersionSelected { get; set; }
        public MSNP12.NotificationServerConnection notificationServerConnectionMSNP12 { get; set; }
        public MSNP15.NotificationServerConnection notificationServerConnectionMSNP15 { get; set; }
        public SwitchboardConnection switchboardConnection { get; set; }
        public int ContactIndexToChat
        {
            get
            {
                switch (MSNPVersionSelected)
                {
                    case "MSNP12":
                        return notificationServerConnectionMSNP12.ContactIndexToChat;
                    case "MSNP15":
                        return notificationServerConnectionMSNP15.ContactIndexToChat;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
            }
            set
            {
                switch (MSNPVersionSelected)
                {
                    case "MSNP12":
                        notificationServerConnectionMSNP12.ContactIndexToChat = value;
                        break;
                    case "MSNP15":
                        notificationServerConnectionMSNP15.ContactIndexToChat = value;
                        break;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
            }
        }
        public string CurrentUserPresenceStatus
        {
            get
            {
                switch (MSNPVersionSelected)
                {
                    case "MSNP12":
                        return notificationServerConnectionMSNP12.CurrentUserPresenceStatus;
                    case "MSNP15":
                        return notificationServerConnectionMSNP15.CurrentUserPresenceStatus;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
            }
            set
            {
                switch (MSNPVersionSelected)
                {
                    case "MSNP12":
                        notificationServerConnectionMSNP12.CurrentUserPresenceStatus = value;
                        break;
                    case "MSNP15":
                        notificationServerConnectionMSNP15.CurrentUserPresenceStatus = value;
                        break;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
            }
        }
        public ObservableCollection<Contact> contact_list
        {
            get
            {
                switch (MSNPVersionSelected)
                {
                    case "MSNP12":
                        return notificationServerConnectionMSNP12.contact_list;
                    case "MSNP15":
                        return notificationServerConnectionMSNP15.contact_list;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
            }
        }
        public ObservableCollection<Contact> contacts_in_forward_list
        {
            get
            {
                switch (MSNPVersionSelected)
                {
                    case "MSNP12":
                        return notificationServerConnectionMSNP12.contacts_in_forward_list;
                    case "MSNP15":
                        return notificationServerConnectionMSNP15.contacts_in_forward_list;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
            }
        }
        public UserInfo userInfo
        {
            get
            {
                switch (MSNPVersionSelected)
                {
                    case "MSNP12":
                        return notificationServerConnectionMSNP12.userInfo;
                    case "MSNP15":
                        return notificationServerConnectionMSNP15.userInfo;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
            }
        }

        public NotificationServerConnection(string version,string escargot_email, string escargot_password)
        {
            MSNPVersionSelected = version;
            switch (MSNPVersionSelected)
            {
                case "MSNP12":
                    notificationServerConnectionMSNP12 = new MSNP12.NotificationServerConnection(escargot_email, escargot_password);
                    break;
                case "MSNP15":
                    notificationServerConnectionMSNP15 = new MSNP15.NotificationServerConnection(escargot_email, escargot_password);
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
        }

        public async Task StartLoginToMessengerAsync()
        {
            switch (MSNPVersionSelected)
            {
                case "MSNP12":
                    await notificationServerConnectionMSNP12.StartLoginToMessengerAsync();
                    break;
                case "MSNP15":
                    await notificationServerConnectionMSNP15.StartLoginToMessengerAsync();
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
        }

        public async Task ChangePresence(string status)
        {
            switch (MSNPVersionSelected)
            {
                case "MSNP12":
                    await notificationServerConnectionMSNP12.ChangePresence(status);
                    break;
                case "MSNP15":
                    throw new NotImplementedException();
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
        }

        public async Task ChangeUserDisplayName(string newDisplayName)
        {
            switch (MSNPVersionSelected)
            {
                case "MSNP12":
                    await notificationServerConnectionMSNP12.ChangeUserDisplayName(newDisplayName);
                    break;
                case "MSNP15":
                    throw new NotImplementedException();
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
        }

        public async Task AddContact(string newContactEmail, string newContactDisplayName = "")
        {
            switch (MSNPVersionSelected)
            {
                case "MSNP12":
                    await notificationServerConnectionMSNP12.AddContact(newContactEmail, newContactDisplayName);
                    break;
                case "MSNP15":
                    throw new NotImplementedException();
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
        }

        public async Task RemoveContact(Contact contactToRemove)
        {
            switch (MSNPVersionSelected)
            {
                case "MSNP12":
                    await notificationServerConnectionMSNP12.RemoveContact(contactToRemove);
                    break;
                case "MSNP15":
                    throw new NotImplementedException();
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
        }

        public async Task InitiateSB()
        {
            switch (MSNPVersionSelected)
            {
                case "MSNP12":
                    await notificationServerConnectionMSNP12.InitiateSB();
                    switchboardConnection = new SwitchboardConnection();
                    switchboardConnection.MSNPVersionSelected = "MSNP12";
                    switchboardConnection.switchboardConnectionMSNP12 = notificationServerConnectionMSNP12.SBConnection;
                    break;
                case "MSNP15":
                    throw new NotImplementedException();
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
        }

        public void Exit()
        {
            switch (MSNPVersionSelected)
            {
                case "MSNP12":
                    notificationServerConnectionMSNP12.Exit();
                    break;
                case "MSNP15":
                    throw new NotImplementedException();
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
        }
    }
}
