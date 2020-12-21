using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml;
using System.IO;
using Windows.UI.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UWPMessengerClient.MSNP
{
    public partial class NotificationServerConnection : INotifyPropertyChanged
    {
        protected SocketCommands NSSocket;
        public SwitchboardConnection SBConnection { get; set; }
        //notification server(escargot) address and address for SSO auth
        protected string NSaddress = "m1.escargot.log1p.xyz";
        protected string RST_address = "https://m1.escargot.log1p.xyz/RST.srf";
        protected string nexus_address = "https://m1.escargot.log1p.xyz/nexus-mock";
        //local addresses are 127.0.0.1 for NSaddress, http://localhost/RST.srf for RST_address
        //and http://localhost/nexus-mock for nexus_address
        protected readonly int port = 1863;
        private string email;
        private string password;
        protected bool _UsingLocalhost = false;
        protected string _MSNPVersion;
        protected int transactionID = 0;
        public int ContactIndexToChat { get; set; }
        public string CurrentUserPresenceStatus { get; set; }
        public bool UsingLocalhost { get => _UsingLocalhost; }
        public string MSNPVersion { get => _MSNPVersion; }
        public UserInfo userInfo { get; set; } = new UserInfo();
        public event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<string> _errorLog = new ObservableCollection<string>();
        public ObservableCollection<string> errorLog
        {
            get => _errorLog;
            set
            {
                _errorLog = value;
                NotifyPropertyChanged();
            }
        }

        public NotificationServerConnection(string messenger_email, string messenger_password, bool use_localhost, string msnp_version)
        {
            email = messenger_email;
            password = messenger_password;
            _UsingLocalhost = use_localhost;
            _MSNPVersion = msnp_version;
            if (_UsingLocalhost)
            {
                NSaddress = "127.0.0.1";
                RST_address = "http://localhost/RST.srf";
                nexus_address = "http://localhost/nexus-mock";
                SharingService_url = "http://localhost/abservice/SharingService.asmx";
                abservice_url = "http://localhost/abservice/abservice.asmx";
                //setting local addresses
            }
        }

        public async Task LoginToMessengerAsync()
        {
            switch (MSNPVersion)
            {
                case "MSNP12":
                    await MSNP12LoginToMessengerAsync();
                    break;
                case "MSNP15":
                    await MSNP15LoginToMessengerAsync();
                    break;
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static HttpWebRequest CreateSOAPRequest(string soap_action, string address)
        {
            HttpWebRequest request = WebRequest.CreateHttp(address);
            request.Headers.Add($@"SOAPAction:{soap_action}");
            request.ContentType = "text/xml;charset=\"utf-8\"";
            request.Accept = "text/xml";
            request.Method = "POST";
            return request;
        }

        public static string MakeSOAPRequest(string SOAP_body, string address, string soap_action)
        {
            HttpWebRequest SOAPRequest = CreateSOAPRequest(soap_action, address);
            XmlDocument SoapXMLBody = new XmlDocument();
            SoapXMLBody.LoadXml(SOAP_body);
            using (Stream stream = SOAPRequest.GetRequestStream())
            {
                SoapXMLBody.Save(stream);
            }
            using (WebResponse webResponse = SOAPRequest.GetResponse())
            {
                using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                {
                    var result = rd.ReadToEnd();
                    return result;
                }
            }
        }

        public void FillForwardListCollection()
        {
            foreach (Contact contact in contact_list)
            {
                if (contact.onForward == true)
                {
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        contacts_in_forward_list.Add(contact);
                    });
                }
            }
        }

        public async Task ChangePresence(string status)
        {
            if (status == "") { throw new ArgumentNullException("Status is empty"); }
            Action changePresence = new Action(() =>
            {
                transactionID++;
                NSSocket.SendCommand($"CHG {transactionID} {status} 0\r\n");
            });
            CurrentUserPresenceStatus = status;
            await Task.Run(changePresence);
        }

        public async Task ChangeUserDisplayName(string newDisplayName)
        {
            if (newDisplayName == "") { throw new ArgumentNullException("Display name is empty"); }
            string ab_display_name_change_xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
                           xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                           xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
                           xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"">
                <soap:Header>
                    <ABApplicationHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <ApplicationId>CFE80F9D-180F-4399-82AB-413F33A1FA11</ApplicationId>
                        <IsMigration>false</IsMigration>
                        <PartnerScenario>Timer</PartnerScenario>
                    </ABApplicationHeader>
                    <ABAuthHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <ManagedGroupRequest>false</ManagedGroupRequest>
                        <TicketToken>{TicketToken}</TicketToken>
                    </ABAuthHeader>
                </soap:Header>
                <soap:Body>
                    <ABContactUpdate xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <abId>00000000-0000-0000-0000-000000000000</abId>
                        <contacts>
                            <Contact xmlns=""http://www.msn.com/webservices/AddressBook"">
                                <contactInfo>
                                    <contactType>Me</contactType>
                                    <displayName>{newDisplayName}</displayName>
                                </contactInfo>
                                <propertiesChanged>DisplayName</propertiesChanged>
                            </Contact>
                        </contacts>
                    </ABContactUpdate>
                </soap:Body>
            </soap:Envelope>";
            if (MSNPVersion == "MSNP15")
            {
                MakeSOAPRequest(ab_display_name_change_xml, abservice_url, "http://www.msn.com/webservices/AddressBook/ABContactUpdate");
            }
            string urlEncodedNewDisplayName = Uri.EscapeUriString(newDisplayName);
            transactionID++;
            await Task.Run(() => NSSocket.SendCommand($"PRP {transactionID} MFN {urlEncodedNewDisplayName}\r\n"));
        }

        public async Task SendUserPersonalMessage(string newPersonalMessage)
        {
            Action psm_action = new Action(() =>
            {
                string encodedPersonalMessage = newPersonalMessage.Replace("&", "&amp;");
                string psm_payload = $@"<Data><PSM>{encodedPersonalMessage}</PSM><CurrentMedia></CurrentMedia></Data>";
                int payload_length = Encoding.UTF8.GetBytes(psm_payload).Length;
                transactionID++;
                NSSocket.SendCommand($"UUX {transactionID} {payload_length}\r\n" + psm_payload);
                Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    userInfo.personalMessage = newPersonalMessage;
                });
            });
            await Task.Run(psm_action);
        }

        public async Task InitiateSB()
        {
            SwitchboardConnection switchboardConnection = new SwitchboardConnection(email, userInfo.displayName);
            SBConnection = switchboardConnection;
            transactionID++;
            await Task.Run(() => NSSocket.SendCommand($"XFR {transactionID} SB\r\n"));
        }

        public void Exit()
        {
            NSSocket.SendCommand("OUT\r\n");
            NSSocket.CloseSocket();
        }

        ~NotificationServerConnection()
        {
            Exit();
        }
    }
}
