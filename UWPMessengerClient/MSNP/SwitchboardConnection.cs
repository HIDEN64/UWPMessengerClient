using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UWPMessengerClient.MSNP
{
    public partial class SwitchboardConnection : INotifyPropertyChanged
    {
        private SocketCommands SBSocket;
        private string SBAddress;
        private int SBPort = 0;
        private string UserEmail;
        private string TrID;
        private string SessionID;
        protected int transactionID = 0;
        public UserInfo PrincipalInfo { get; set; } = new UserInfo();
        public UserInfo userInfo { get; set; } = new UserInfo();
        public bool connected { get; set; }
        public int principalsConnected { get; set; }
        public string outputString { get; set; }
        public byte[] outputBuffer { get; set; } = new byte[4096];
        private bool waitingTyping = false;
        public event PropertyChangedEventHandler PropertyChanged;
        Dictionary<string, Action> command_handlers;
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

        public SwitchboardConnection(string email, string userDisplayName)
        {
            command_handlers = new Dictionary<string, Action>()
            {
                {"USR", () => HandleUSR() },
                {"ANS", () => HandleANS() },
                {"JOI", () => principalsConnected++ },
                {"IRO", () => principalsConnected++ }
            };
            UserEmail = email;
            userInfo.displayName = userDisplayName;
        }

        public SwitchboardConnection(string address, int port, string email, string trID, string userDisplayName)
        {
            command_handlers = new Dictionary<string, Action>()
            {
                {"USR", () => HandleUSR() },
                {"ANS", () => HandleANS() },
                {"JOI", () => principalsConnected++ },
                {"IRO", () => principalsConnected++ }
            };
            SBAddress = address;
            SBPort = port;
            UserEmail = email;
            TrID = trID;
            userInfo.displayName = userDisplayName;
        }

        public SwitchboardConnection(string address, int port, string email, string trID, string userDisplayName, string principalDisplayName, string sessionID)
        {
            command_handlers = new Dictionary<string, Action>()
            {
                {"USR", () => HandleUSR() },
                {"ANS", () => HandleANS() },
                {"JOI", () => principalsConnected++ },
                {"IRO", () => principalsConnected++ }
            };
            SBAddress = address;
            SBPort = port;
            UserEmail = email;
            TrID = trID;
            SessionID = sessionID;
            userInfo.displayName = userDisplayName;
            PrincipalInfo.displayName = principalDisplayName;
        }

        public void SetAddressPortAndTrID(string address, int port, string trID)
        {
            SBAddress = address;
            SBPort = port;
            TrID = trID;
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task AddToErrorLog(string error)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                errorLog.Add(error);
            });
        }

        public async Task LoginToNewSwitchboardAsync()
        {
            Action sbconnect = new Action(() =>
            {
                SBSocket = new SocketCommands(SBAddress, SBPort);
                SBSocket.ConnectSocket();
                SBSocket.BeginReceiving(outputBuffer, new AsyncCallback(ReceivingCallback), this);
                transactionID++;
                SBSocket.SendCommand($"USR {transactionID} {UserEmail} {TrID}\r\n");
            });
            await Task.Run(sbconnect);
            connected = true;
        }

        public async Task InvitePrincipal(string principal_email)
        {
            if (connected)
            {
                transactionID++;
                await Task.Run(() =>
                {
                    SBSocket.SendCommand($"CAL {transactionID} {principal_email}\r\n");
                });
            }
            else
            {
                throw new Exceptions.NotConnectedException();
            }
        }

        public async Task InvitePrincipal(string principal_email, string principal_display_name)
        {
            if (connected)
            {
                transactionID++;
                await Task.Run(() =>
                {
                    SBSocket.SendCommand($"CAL {transactionID} {principal_email}\r\n");
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        PrincipalInfo.displayName = principal_display_name;
                    });
                });
            }
            else
            {
                throw new Exception();
            }
        }

        public async Task SendMessage(string message_text)
        {
            if (connected && principalsConnected > 0)
            {
                Action message_action = new Action(() =>
                {
                    string message = "MIME-Version: 1.0\r\nContent-Type: text/plain; charset=UTF-8\r\nX-MMS-IM-Format: FN=Arial; EF=; CO=0; CS=0; PF=22\r\n\r\n" + message_text;
                    byte[] byte_message = Encoding.UTF8.GetBytes(message);
                    transactionID++;
                    SBSocket.SendCommand($"MSG {transactionID} N {byte_message.Length}\r\n{message}");
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MessageList.Add(new Message() { message_text = message_text, sender = userInfo.displayName });
                    });
                });
                try
                {
                    await Task.Run(message_action);
                }
                catch (AggregateException ae)
                {
                    foreach (Exception ie in ae.InnerExceptions)
                    {
                        Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            MessageList.Add(new Message() { message_text = "There was an error sending this message: " + ie.Message, sender = "Error" });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MessageList.Add(new Message() { message_text = "There was an error sending this message: " + ex.Message, sender = "Error" });
                    });
                }
            }
        }

        public async Task SendTypingUser()
        {
            if (connected && principalsConnected > 0 && !waitingTyping)
            {
                await Task.Run(() =>
                {
                    string message = $"MIME-Version: 1.0\r\nContent-Type: text/x-msmsgscontrol\r\nTypingUser: {UserEmail}\r\n\r\n\r\n";
                    byte[] byte_message = Encoding.UTF8.GetBytes(message);
                    transactionID++;
                    SBSocket.SendCommand($"MSG {transactionID} U {byte_message.Length}\r\n{message}");
                });
                waitingTyping = true;
                await Task.Delay(5000);
                waitingTyping = false;
            }
        }

        public async Task AnswerRNG()
        {
            await Task.Run(() =>
            {
                SBSocket = new SocketCommands(SBAddress, SBPort);
                SBSocket.ConnectSocket();
                SBSocket.BeginReceiving(outputBuffer, new AsyncCallback(ReceivingCallback), this);
                transactionID++;
                SBSocket.SendCommand($"ANS {transactionID} {UserEmail} {TrID} {SessionID}\r\n");
            });
        }

        public void Exit()
        {
            SBSocket.SendCommand("OUT\r\n");
            SBSocket.CloseSocket();
        }

        ~SwitchboardConnection()
        {
            Exit();
        }
    }
}
