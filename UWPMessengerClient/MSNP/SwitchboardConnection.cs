using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace UWPMessengerClient.MSNP
{
    public partial class SwitchboardConnection : INotifyPropertyChanged
    {
        protected SocketCommands SBSocket;
        protected string SBAddress;
        protected int SBPort = 0;
        protected string AuthString;
        protected string SessionID;
        protected int transactionID = 0;
        public UserInfo PrincipalInfo { get; set; } = new UserInfo();
        public UserInfo userInfo { get; set; } = new UserInfo();
        public bool connected { get; set; }
        public int principalsConnected { get; set; }
        public string outputString { get; set; }
        public byte[] outputBuffer { get; set; } = new byte[4096];
        public bool KeepMessagingHistory { get; set; } = true;
        protected bool waitingTyping = false;
        protected bool waitingNudge = false;
        protected int MaximumInkSize = 1140;
        private static Random random = new Random();
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler HistoryLoaded;
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
                {"IRO", () => principalsConnected++ },
                {"MSG", () => HandleMSG() }
            };
            userInfo.Email = email;
            userInfo.displayName = userDisplayName;
        }

        public SwitchboardConnection(string address, int port, string email, string authString, string userDisplayName)
        {
            command_handlers = new Dictionary<string, Action>()
            {
                {"USR", () => HandleUSR() },
                {"ANS", () => HandleANS() },
                {"JOI", () => principalsConnected++ },
                {"IRO", () => principalsConnected++ },
                {"MSG", () => HandleMSG() }
            };
            SBAddress = address;
            SBPort = port;
            userInfo.Email = email;
            AuthString = authString;
            userInfo.displayName = userDisplayName;
        }

        public SwitchboardConnection(string address, int port, string email, string authString, string userDisplayName, string principalDisplayName, string principalEmail, string sessionID)
        {
            command_handlers = new Dictionary<string, Action>()
            {
                {"USR", () => HandleUSR() },
                {"ANS", () => HandleANS() },
                {"JOI", () => principalsConnected++ },
                {"IRO", () => principalsConnected++ },
                {"MSG", () => HandleMSG() }
            };
            SBAddress = address;
            SBPort = port;
            userInfo.Email = email;
            AuthString = authString;
            SessionID = sessionID;
            userInfo.displayName = userDisplayName;
            PrincipalInfo.displayName = principalDisplayName;
            PrincipalInfo.Email = principalEmail;
        }

        public void SetAddressPortAndAuthString(string address, int port, string AuthString)
        {
            SBAddress = address;
            SBPort = port;
            this.AuthString = AuthString;
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
                SBSocket.SendCommand($"USR {transactionID} {userInfo.Email} {AuthString}\r\n");
            });
            await Task.Run(sbconnect);
            connected = true;
        }

        public void FillMessageHistory()
        {
            if (KeepMessagingHistory)
            {
                List<string> JSONMessages = DatabaseAccess.ReturnMessagesFromSenderAndReceiver(userInfo.Email, PrincipalInfo.Email);
                foreach (string JSONMessage in JSONMessages)
                {
                    Message pastMessage = JsonConvert.DeserializeObject<Message>(JSONMessage);
                    pastMessage.IsHistory = true;
                    var task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MessageList.Add(pastMessage);
                    });
                }
                HistoryLoaded?.Invoke(this, new EventArgs());
            }
        }

        public async Task InvitePrincipal(string principal_email)
        {
            if (connected)
            {
                PrincipalInfo.Email = principal_email;
                transactionID++;
                await Task.Run(() =>
                {
                    SBSocket.SendCommand($"CAL {transactionID} {principal_email}\r\n");
                    FillMessageHistory();
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
                PrincipalInfo.Email = principal_email;
                transactionID++;
                await Task.Run(() =>
                {
                    SBSocket.SendCommand($"CAL {transactionID} {principal_email}\r\n");
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        PrincipalInfo.displayName = principal_display_name;
                    });
                    FillMessageHistory();
                });
            }
            else
            {
                throw new Exceptions.NotConnectedException();
            }
        }

        public async Task SendTextMessage(string message_text)
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
                        Message newMessage = new Message() { message_text = message_text, sender = userInfo.displayName, receiver = PrincipalInfo.displayName, sender_email = userInfo.Email, receiver_email = PrincipalInfo.Email, IsHistory = false };
                        MessageList.Add(newMessage);
                        DatabaseAccess.AddMessageToTable(userInfo.Email, PrincipalInfo.Email, newMessage);
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
                            MessageList.Add(new Message() { message_text = "There was an error sending this message: " + ie.Message, sender = "Error", IsHistory = false });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MessageList.Add(new Message() { message_text = "There was an error sending this message: " + ex.Message, sender = "Error", IsHistory = false });
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
                    string message = $"MIME-Version: 1.0\r\nContent-Type: text/x-msmsgscontrol\r\nTypingUser: {userInfo.Email}\r\n\r\n\r\n";
                    byte[] byte_message = Encoding.UTF8.GetBytes(message);
                    transactionID++;
                    SBSocket.SendCommand($"MSG {transactionID} U {byte_message.Length}\r\n{message}");
                });
                waitingTyping = true;
                //sending TypingUser every 5 seconds only
                await Task.Delay(5000);
                waitingTyping = false;
            }
        }

        public async Task SendNudge()
        {
            if (connected && principalsConnected > 0)
            {
                if (!waitingNudge)
                {
                    try
                    {
                        await Task.Run(() =>
                        {
                            string nudge_message = "MIME-Version: 1.0\r\nContent-Type: text/x-msnmsgr-datacast\r\n\r\nID: 1\r\n";
                            byte[] byte_message = Encoding.UTF8.GetBytes(nudge_message);
                            transactionID++;
                            SBSocket.SendCommand($"MSG {transactionID} A {byte_message.Length}\r\n{nudge_message}");
                        });
                        string nudge_text = $"You sent {PrincipalInfo.displayName} a nudge";
                        Message newMessage = new Message() { message_text = nudge_text, receiver = PrincipalInfo.displayName, sender_email = userInfo.Email, receiver_email = PrincipalInfo.Email, IsHistory = false };
                        AddToMessageList(newMessage);
                    }
                    catch (Exception ex)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            MessageList.Add(new Message() { message_text = "There was an error sending this message: " + ex.Message, sender = "Error", IsHistory = false });
                        });
                    }
                    waitingNudge = true;
                    //12 second cooldown, about the same as the wlm 2009 client
                    await Task.Delay(12000);
                    waitingNudge = false;
                }
                else
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MessageList.Add(new Message() { message_text = "Wait before sending nudge again", sender = "", IsHistory = false });
                    });
                }
            }
        }

        public static string GenerateMessageID()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            List<string> sets = new List<string>();
            sets.Add(new string(Enumerable.Repeat(chars, 8)
              .Select(s => s[random.Next(s.Length)]).ToArray()));
            sets.Add(new string(Enumerable.Repeat(chars, 4)
              .Select(s => s[random.Next(s.Length)]).ToArray()));
            sets.Add(new string(Enumerable.Repeat(chars, 4)
              .Select(s => s[random.Next(s.Length)]).ToArray()));
            sets.Add(new string(Enumerable.Repeat(chars, 4)
              .Select(s => s[random.Next(s.Length)]).ToArray()));
            sets.Add(new string(Enumerable.Repeat(chars, 12)
              .Select(s => s[random.Next(s.Length)]).ToArray()));
            StringBuilder stringBuilder = new StringBuilder(38);
            stringBuilder.Append("{");
            stringBuilder.Append(sets[0]);
            for (int i = 1; i < sets.Count; i++)
            {
                stringBuilder.Append("-" + sets[i]);
            }
            stringBuilder.Append("}");
            string message_id = stringBuilder.ToString();
            return message_id;
        }

        public List<InkChunk> DivideInkIntoChunks(byte[] ink_bytes, string MessageId)
        {
            List<InkChunk> InkChunks = new List<InkChunk>();
            double NumberOfChunksDouble = ink_bytes.Length / MaximumInkSize;
            NumberOfChunksDouble = Math.Ceiling(NumberOfChunksDouble);
            int NumberOfChunks = Convert.ToInt32(NumberOfChunksDouble);
            int NumberOfFullChunks = NumberOfChunks;
            if (NumberOfChunks % MaximumInkSize > 0)
            {
                NumberOfFullChunks--;
            }
            int ink_pos = 0;
            byte[] ink_chunk = new byte[MaximumInkSize];
            Buffer.BlockCopy(ink_bytes, ink_pos, ink_chunk, 0, MaximumInkSize);
            InkChunks.Add(new InkChunk() { ChunkNumber = 0, MessageID = MessageId, EncodedChunk = "base64:" + Convert.ToBase64String(ink_chunk) });
            ink_pos += MaximumInkSize;
            for (int i = 1; i <= NumberOfFullChunks; i++)
            {
                Buffer.BlockCopy(ink_bytes, ink_pos, ink_chunk, 0, MaximumInkSize);
                InkChunks.Add(new InkChunk() { ChunkNumber = i, MessageID = MessageId, EncodedChunk = Convert.ToBase64String(ink_chunk) });
                ink_pos += MaximumInkSize;
            }
            int LastChunkLen = ink_bytes.Length - ink_pos;
            ink_chunk = new byte[LastChunkLen];
            Buffer.BlockCopy(ink_bytes, ink_pos, ink_chunk, 0, LastChunkLen);
            InkChunks.Add(new InkChunk() { ChunkNumber = NumberOfChunks, MessageID = MessageId, EncodedChunk = Convert.ToBase64String(ink_chunk) });
            ink_pos += LastChunkLen;
            return InkChunks;
        }

        public async Task SendInk(byte[] ink_bytes)
        {
            if (ink_bytes.Length > MaximumInkSize)
            {
                string MessageId = GenerateMessageID();
                List<InkChunk> InkChunks = DivideInkIntoChunks(ink_bytes, MessageId);
                transactionID++;
                string InkChunkMessagePayload = $"Mime-Version: 1.0\r\nContent-Type: application/x-ms-ink\r\nMessage-ID: {MessageId}\r\nChunks: {InkChunks.Count}\r\n\r\n{InkChunks[0].EncodedChunk}";
                string InkChunkMessage = $"MSG {transactionID} N {Encoding.UTF8.GetBytes(InkChunkMessagePayload).Length}\r\n{InkChunkMessagePayload}";
                try
                {
                    SBSocket.SendCommand(InkChunkMessage);
                }
                catch (Exception ex)
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MessageList.Add(new Message() { message_text = "There was an error sending this message: " + ex.Message, sender = "Error", IsHistory = false });
                    });
                }
                for (int i = 1; i < InkChunks.Count; i++)
                {
                    transactionID++;
                    InkChunkMessagePayload = $"Message-ID: {MessageId}\r\nChunk: {InkChunks[i].ChunkNumber}\r\n\r\n{InkChunks[i].EncodedChunk}";
                    InkChunkMessage = $"MSG {transactionID} N {Encoding.UTF8.GetBytes(InkChunkMessagePayload).Length}\r\n{InkChunkMessagePayload}";
                    try
                    {
                        SBSocket.SendCommand(InkChunkMessage);
                    }
                    catch (Exception ex)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            MessageList.Add(new Message() { message_text = "There was an error sending this message: " + ex.Message, sender = "Error", IsHistory = false });
                        });
                    }
                }
            }
            else
            {
                transactionID++;
                string InkChunkMessagePayload = $"Mime-Version: 1.0\r\nContent-Type: application/x-ms-ink\r\n\r\n{"base64:" + Convert.ToBase64String(ink_bytes)}";
                string InkChunkMessage = $"MSG {transactionID} N {Encoding.UTF8.GetBytes(InkChunkMessagePayload).Length}\r\n{InkChunkMessagePayload}";
                try
                {
                    SBSocket.SendCommand(InkChunkMessage);
                }
                catch (Exception ex)
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MessageList.Add(new Message() { message_text = "There was an error sending this message: " + ex.Message, sender = "Error", IsHistory = false });
                    });
                }
            }
            //sends ink in ISF format
        }

        public async Task AnswerRNG()
        {
            await Task.Run(() =>
            {
                SBSocket = new SocketCommands(SBAddress, SBPort);
                SBSocket.ConnectSocket();
                SBSocket.BeginReceiving(outputBuffer, new AsyncCallback(ReceivingCallback), this);
                transactionID++;
                SBSocket.SendCommand($"ANS {transactionID} {userInfo.Email} {AuthString} {SessionID}\r\n");
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
