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
using System.Text.RegularExpressions;
using UWPMessengerClient.MSNP.Exceptions;
using UWPMessengerClient.MSNP.SOAP;
using System.Net.Sockets;

namespace UWPMessengerClient.MSNP
{
    public partial class NotificationServerConnection : INotifyPropertyChanged
    {
        protected SocketCommands NSSocket;
        public SwitchboardConnection SBConnection { get; set; }
        //notification server(escargot) address and address for SSO auth
        protected string NSaddress = "m1.escargot.log1p.xyz";
        protected string nexus_address = "https://m1.escargot.log1p.xyz/nexus-mock";
        //local addresses are 127.0.0.1 for NSaddress, http://localhost/RST.srf for RST_address
        //and http://localhost/nexus-mock for nexus_address
        protected readonly int port = 1863;
        private string email;
        private string password;
        protected Regex PlusCharactersRegex = new Regex("\\[(.*?)\\]");
        public bool UsingLocalhost { get; protected set; } = false;
        public string MSNPVersion { get; protected set; } = "MSNP15";
        protected int transactionID = 0;
        protected uint clientCapabilities = 0x84140420;
        public int ContactIndexToChat { get; set; }
        public string UserPresenceStatus { get; set; }
        public bool KeepMessagingHistoryInSwitchboard { get; set; } = true;
        public UserInfo userInfo { get; set; } = new UserInfo();
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<EventArgs> NotConnected;
        protected Dictionary<string, Action> command_handlers;
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

        public NotificationServerConnection(string messenger_email, string messenger_password, bool use_localhost, string msnp_version, string initial_status = PresenceStatuses.Available)
        {
            command_handlers = new Dictionary<string, Action>()
            {
                {"LST", () => HandleLST() },
                {"ADC", () => HandleADC() },
                {"PRP", () => HandlePRP() },
                {"ILN", () => HandleILN() },
                {"NLN", () => HandleNLN() },
                {"FLN", () => HandleFLN() },
                {"UBX", () => HandleUBX() },
                {"XFR", async () => await HandleXFR() },
                {"RNG", () => HandleRNG() }
            };
            email = messenger_email;
            password = messenger_password;
            UsingLocalhost = use_localhost;
            MSNPVersion = msnp_version;
            UserPresenceStatus = initial_status;
            if (UsingLocalhost)
            {
                NSaddress = "127.0.0.1";
                nexus_address = "http://localhost/nexus-mock";
                //setting local addresses
            }
            SOAPRequests = new SOAPRequests(UsingLocalhost);
        }

        public async Task AddToErrorLog(string error)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                errorLog.Add(error);
            });
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
#pragma warning disable CS4014 // Como esta chamada não é esperada, a execução do método atual continua antes de a chamada ser concluída
            Ping();
#pragma warning restore CS4014 // Como esta chamada não é esperada, a execução do método atual continua antes de a chamada ser concluída
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void FillForwardListCollection()
        {
            foreach (Contact contact in contact_list)
            {
                if (contact.onForward)
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
                NSSocket.SendCommand($"CHG {transactionID} {status} {clientCapabilities}\r\n");
            });
            UserPresenceStatus = status;
            await Task.Run(changePresence);
        }

        public async Task ChangeUserDisplayName(string newDisplayName)
        {
            if (newDisplayName == "") { throw new ArgumentNullException("Display name is empty"); }
            if (MSNPVersion == "MSNP15")
            {
                SOAPRequests.MakeChangeUserDisplayNameSOAPRequest(newDisplayName);
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

        public async Task Ping()
        {
            bool IsConnected;
            do
            {
                IsConnected = await Task.Run(() =>
                {
                    try
                    {
                        NSSocket.SendCommandWithException("PNG\r\n");
                        return true;
                    }
                    catch (NotConnectedException)
                    {
                        NSSocket.CloseSocket();
                        NotConnected?.Invoke(this, new EventArgs());
                        return false;
                    }
                    catch (SocketException)
                    {
                        NSSocket.CloseSocket();
                        NotConnected?.Invoke(this, new EventArgs());
                        return false;
                    }
                });
                await Task.Delay(60000);
            }
            while (IsConnected);
        }

        public async Task InitiateSB()
        {
            SwitchboardConnection switchboardConnection = new SwitchboardConnection(email, userInfo.displayName);
            SBConnection = switchboardConnection;
            SBConnection.KeepMessagingHistory = KeepMessagingHistoryInSwitchboard;
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
