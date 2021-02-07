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
        public List<SBConversation> SBConversations { get; set; } = new List<SBConversation>();
        //notification server(escargot) address and address for SSO auth
        protected string NSaddress = "m1.escargot.log1p.xyz";
        protected string NexusAddress = "https://m1.escargot.log1p.xyz/nexus-mock";
        //local addresses are 127.0.0.1 for NSaddress and http://localhost/nexus-mock for nexus_address
        protected readonly int Port = 1863;
        private string Email;
        private string Password;
        protected Regex PlusCharactersRegex = new Regex("\\[(.*?)\\]");
        public bool UsingLocalhost { get; protected set; } = false;
        public string MSNPVersion { get; protected set; } = "MSNP15";
        protected int TransactionID = 0;
        protected uint ClientCapabilities = 0x84140428;
        public Contact ContactToChat { get; set; }
        public string UserPresenceStatus { get; set; }
        public bool KeepMessagingHistoryInSwitchboard { get; set; } = true;
        public UserInfo userInfo { get; set; } = new UserInfo();
        private static Random random = new Random();
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler NotConnected;
        protected Dictionary<string, Action> CommandHandlers;
        private ObservableCollection<string> _errorLog = new ObservableCollection<string>();
        public ObservableCollection<string> ErrorLog
        {
            get => _errorLog;
            private set
            {
                _errorLog = value;
                NotifyPropertyChanged();
            }
        }

        public NotificationServerConnection()
        {
            CommandHandlers = new Dictionary<string, Action>()
            {
                {"LST", () => HandleLST() },
                {"ADC", () => HandleADC() },
                {"ADL", () => HandleADL() },
                {"PRP", () => HandlePRP() },
                {"ILN", () => HandleILN() },
                {"NLN", () => HandleNLN() },
                {"FLN", () => HandleFLN() },
                {"UBX", () => HandleUBX() },
                {"XFR", async () => await HandleXFR() },
                {"RNG", () => HandleRNG() }
            };
        }

        public NotificationServerConnection(string messenger_email, string messenger_password, bool use_localhost, string msnp_version, string initial_status = PresenceStatuses.Available)
        {
            CommandHandlers = new Dictionary<string, Action>()
            {
                {"LST", () => HandleLST() },
                {"ADC", () => HandleADC() },
                {"ADL", () => HandleADL() },
                {"PRP", () => HandlePRP() },
                {"ILN", () => HandleILN() },
                {"NLN", () => HandleNLN() },
                {"FLN", () => HandleFLN() },
                {"UBX", () => HandleUBX() },
                {"XFR", async () => await HandleXFR() },
                {"RNG", () => HandleRNG() }
            };
            Email = messenger_email;
            Password = messenger_password;
            UsingLocalhost = use_localhost;
            MSNPVersion = msnp_version;
            UserPresenceStatus = initial_status;
            if (UsingLocalhost)
            {
                NSaddress = "127.0.0.1";
                NexusAddress = "http://localhost/nexus-mock";
                //setting local addresses
            }
            SOAPRequests = new SOAPRequests(UsingLocalhost);
        }

        public async Task AddToErrorLog(string error)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ErrorLog.Add(error);
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

        public async Task ChangePresence(string status)
        {
            if (status == "") { throw new ArgumentNullException("Status is empty"); }
            Action changePresence = new Action(() =>
            {
                TransactionID++;
                NSSocket.SendCommand($"CHG {TransactionID} {status} {ClientCapabilities}\r\n");
            });
            UserPresenceStatus = status;
            await Task.Run(changePresence);
        }

        public async Task ChangeUserDisplayName(string newDisplayName)
        {
            if (newDisplayName == "") { throw new ArgumentNullException("Display name is empty"); }
            if (MSNPVersion == "MSNP15")
            {
                SOAPRequests.ChangeUserDisplayNameRequest(newDisplayName);
            }
            string urlEncodedNewDisplayName = Uri.EscapeUriString(newDisplayName);
            TransactionID++;
            await Task.Run(() => NSSocket.SendCommand($"PRP {TransactionID} MFN {urlEncodedNewDisplayName}\r\n"));
        }

        public async Task SendUserPersonalMessage(string newPersonalMessage)
        {
            Action psm_action = new Action(() =>
            {
                string encodedPersonalMessage = newPersonalMessage.Replace("&", "&amp;");
                string psm_payload = $@"<Data><PSM>{encodedPersonalMessage}</PSM><CurrentMedia></CurrentMedia></Data>";
                int payload_length = Encoding.UTF8.GetBytes(psm_payload).Length;
                TransactionID++;
                NSSocket.SendCommand($"UUX {TransactionID} {payload_length}\r\n" + psm_payload);
                Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    userInfo.personalMessage = newPersonalMessage;
                });
            });
            await Task.Run(psm_action);
        }

        public async Task<string> StartChat(Contact contactToChat)
        {
            ContactToChat = contactToChat;
            await InitiateSB();
            int conv_id = random.Next(1000, 9999);
            SBConversation conversation = new SBConversation(this, Convert.ToString(conv_id));
            SBConversations.Add(conversation);
            return conversation.ConversationID;
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

        protected async Task InitiateSB()
        {
            TransactionID++;
            await Task.Run(() => NSSocket.SendCommand($"XFR {TransactionID} SB\r\n"));
        }

        public SBConversation ReturnConversationFromConversationID(string conversation_id)
        {
            var conv_item = SBConversations.FirstOrDefault(sb => sb.ConversationID == conversation_id);
            return conv_item;
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
