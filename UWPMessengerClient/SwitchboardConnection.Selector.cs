using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace UWPMessengerClient
{
    public class SwitchboardConnection
    {
        public string MSNPVersionSelected { get; set; }
        public MSNP12.SwitchboardConnection switchboardConnectionMSNP12 { get; set; }
        public MSNP15.SwitchboardConnection switchboardConnectionMSNP15 { get; set; }
        public bool connected
        {
            get
            {
                switch (MSNPVersionSelected)
                {
                    case "MSNP12":
                        return switchboardConnectionMSNP12.connected;
                    case "MSNP15":
                        throw new NotImplementedException();
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
            }
            set
            {
                switch (MSNPVersionSelected)
                {
                    case "MSNP12":
                        switchboardConnectionMSNP12.connected = value;
                        break;
                    case "MSNP15":
                        throw new NotImplementedException();
                        break;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
            }
        }
        public ObservableCollection<Message> MessageList
        {
            get
            {
                switch (MSNPVersionSelected)
                {
                    case "MSNP12":
                        return switchboardConnectionMSNP12.MessageList;
                    case "MSNP15":
                        throw new NotImplementedException();
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
                        return switchboardConnectionMSNP12.userInfo;
                    case "MSNP15":
                        return switchboardConnectionMSNP12.userInfo;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
            }
        }
        public UserInfo PrincipalInfo
        {
            get
            {
                switch (MSNPVersionSelected)
                {
                    case "MSNP12":
                        return switchboardConnectionMSNP12.PrincipalInfo;
                    case "MSNP15":
                        throw new NotImplementedException();
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
            }
        }

        public async Task SendMessage(string message_text)
        {
            switch (MSNPVersionSelected)
            {
                case "MSNP12":
                    await switchboardConnectionMSNP12.SendMessage(message_text);
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
