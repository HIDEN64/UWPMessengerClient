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
        protected SocketCommands nsSocket;
        public List<SBConversation> SbConversations { get; set; } = new List<SBConversation>();
        //notification server(escargot) address and address for SSO auth
        protected string nsAddress = "m1.escargot.log1p.xyz";
        protected string nexusAddress = "https://m1.escargot.log1p.xyz/nexus-mock";
        //local addresses are 127.0.0.1 for NSaddress and http://localhost/nexus-mock for nexus_address
        protected readonly int port = 1863;
        private string email;
        private string password;
        protected Regex plusCharactersRegex = new Regex("\\[(.*?)\\]");
        public bool UsingLocalhost { get; protected set; } = false;
        public string MsnpVersion { get; protected set; } = "MSNP15";
        protected int transactionId = 0;
        protected uint clientCapabilities = 0x84140428;
        public Contact ContactToChat { get; set; }
        public string UserPresenceStatus { get; set; }
        public bool KeepMessagingHistoryInSwitchboard { get; set; } = true;
        public UserInfo UserInfo { get; set; } = new UserInfo();
        private static Random random = new Random();
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler NotConnected;
        protected Dictionary<string, Action> CommandHandlers;
        private ObservableCollection<string> errorLog = new ObservableCollection<string>();
        public ObservableCollection<string> ErrorLog
        {
            get => errorLog;
            private set
            {
                errorLog = value;
                NotifyPropertyChanged();
            }
        }

        public NotificationServerConnection()
        {
            CommandHandlers = new Dictionary<string, Action>()
            {
                {"LST", () => HandleLst() },
                {"ADC", () => HandleAdc() },
                {"ADL", () => HandleAdl() },
                {"PRP", () => HandlePrp() },
                {"ILN", () => HandleIln() },
                {"NLN", () => HandleNln() },
                {"FLN", () => HandleFln() },
                {"UBX", () => HandleUbx() },
                {"XFR", async () => await HandleXfr() },
                {"RNG", () => HandleRng() }
            };
        }

        public NotificationServerConnection(string messengerEmail, string messengerPassword, bool useLocalhost, string msnpVersion, string initialStatus = PresenceStatuses.Available)
        {
            CommandHandlers = new Dictionary<string, Action>()
            {
                {"LST", () => HandleLst() },
                {"ADC", () => HandleAdc() },
                {"ADL", () => HandleAdl() },
                {"PRP", () => HandlePrp() },
                {"ILN", () => HandleIln() },
                {"NLN", () => HandleNln() },
                {"FLN", () => HandleFln() },
                {"UBX", () => HandleUbx() },
                {"XFR", async () => await HandleXfr() },
                {"RNG", () => HandleRng() }
            };
            email = messengerEmail;
            password = messengerPassword;
            UsingLocalhost = useLocalhost;
            MsnpVersion = msnpVersion;
            UserPresenceStatus = initialStatus;
            if (UsingLocalhost)
            {
                nsAddress = "127.0.0.1";
                nexusAddress = "http://localhost/nexus-mock";
                //setting local addresses
            }
            soapRequests = new SOAPRequests(UsingLocalhost);
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
            switch (MsnpVersion)
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
                transactionId++;
                nsSocket.SendCommand($"CHG {transactionId} {status} {clientCapabilities}\r\n");
            });
            UserPresenceStatus = status;
            await Task.Run(changePresence);
        }

        public async Task ChangeUserDisplayName(string newDisplayName)
        {
            if (newDisplayName == "") { throw new ArgumentNullException("Display name is empty"); }
            if (MsnpVersion == "MSNP15")
            {
                soapRequests.ChangeUserDisplayNameRequest(newDisplayName);
            }
            string urlEncodedNewDisplayName = Uri.EscapeUriString(newDisplayName);
            transactionId++;
            await Task.Run(() => nsSocket.SendCommand($"PRP {transactionId} MFN {urlEncodedNewDisplayName}\r\n"));
        }

        public async Task SendUserPersonalMessage(string newPersonalMessage)
        {
            Action psmAction = new Action(() =>
            {
                string encodedPersonalMessage = newPersonalMessage.Replace("&", "&amp;");
                string psmPayload = $@"<Data><PSM>{encodedPersonalMessage}</PSM><CurrentMedia></CurrentMedia></Data>";
                int payloadLength = Encoding.UTF8.GetBytes(psmPayload).Length;
                transactionId++;
                nsSocket.SendCommand($"UUX {transactionId} {payloadLength}\r\n" + psmPayload);
                Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UserInfo.PersonalMessage = newPersonalMessage;
                });
            });
            await Task.Run(psmAction);
        }

        public async Task<string> StartChat(Contact contactToChat)
        {
            ContactToChat = contactToChat;
            await InitiateSB();
            int conversationId = random.Next(1000, 9999);
            SBConversation conversation = new SBConversation(this, Convert.ToString(conversationId));
            SbConversations.Add(conversation);
            return conversation.ConversationId;
        }

        public async Task Ping()
        {
            bool isConnected;
            do
            {
                isConnected = await Task.Run(() =>
                {
                    try
                    {
                        nsSocket.SendCommandWithException("PNG\r\n");
                        return true;
                    }
                    catch (NotConnectedException)
                    {
                        nsSocket.CloseSocket();
                        NotConnected?.Invoke(this, new EventArgs());
                        return false;
                    }
                    catch (SocketException)
                    {
                        nsSocket.CloseSocket();
                        NotConnected?.Invoke(this, new EventArgs());
                        return false;
                    }
                });
                await Task.Delay(60000);
            }
            while (isConnected);
        }

        protected async Task InitiateSB()
        {
            transactionId++;
            await Task.Run(() => nsSocket.SendCommand($"XFR {transactionId} SB\r\n"));
        }

        public SBConversation ReturnConversationFromConversationId(string conversationId)
        {
            var conversationItem = SbConversations.FirstOrDefault(sb => sb.ConversationId == conversationId);
            return conversationItem;
        }

        public void Exit()
        {
            nsSocket.SendCommand("OUT\r\n");
            nsSocket.CloseSocket();
        }

        ~NotificationServerConnection()
        {
            Exit();
        }
    }
}
