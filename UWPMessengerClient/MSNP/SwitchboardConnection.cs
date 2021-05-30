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
        protected SocketCommands sbSocket;
        protected string sbAddress;
        protected int sbPort = 0;
        protected int transactionId = 0;
        protected string authString;
        public string SessionID { get; protected set; }
        public UserInfo PrincipalInfo { get; set; } = new UserInfo();
        public UserInfo UserInfo { get; set; } = new UserInfo();
        public bool Connected { get; set; }
        public int PrincipalsConnected { get; set; }
        public string OutputString { get; set; }
        public byte[] OutputBuffer { get; set; } = new byte[4096];
        public bool KeepMessagingHistory { get; set; } = true;
        protected bool waitingTyping = false;
        protected bool waitingNudge = false;
        protected int maximumInkSize = 1140;
        private static Random random = new Random();
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler HistoryLoaded;
        Dictionary<string, Action> commandHandlers;
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

        public SwitchboardConnection(string address, int port, string email, string authString, string userDisplayName)
        {
            commandHandlers = new Dictionary<string, Action>()
            {
                {"USR", () => HandleUSR() },
                {"ANS", () => HandleANS() },
                {"CAL", () => HandleCAL() },
                {"JOI", () => PrincipalsConnected++ },
                {"IRO", () => PrincipalsConnected++ },
                {"MSG", () => HandleMSG() }
            };
            sbAddress = address;
            sbPort = port;
            UserInfo.Email = email;
            this.authString = authString;
            UserInfo.displayName = userDisplayName;
        }

        public SwitchboardConnection(string address, int port, string email, string authString, string userDisplayName, string principalDisplayName, string principalEmail, string sessionID)
        {
            commandHandlers = new Dictionary<string, Action>()
            {
                {"USR", () => HandleUSR() },
                {"ANS", () => HandleANS() },
                {"CAL", () => HandleCAL() },
                {"JOI", () => PrincipalsConnected++ },
                {"IRO", () => PrincipalsConnected++ },
                {"MSG", () => HandleMSG() }
            };
            sbAddress = address;
            sbPort = port;
            UserInfo.Email = email;
            this.authString = authString;
            SessionID = sessionID;
            UserInfo.displayName = userDisplayName;
            PrincipalInfo.displayName = principalDisplayName;
            PrincipalInfo.Email = principalEmail;
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task AddToErrorLog(string error)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ErrorLog.Add(error);
            });
        }

        public async Task LoginToNewSwitchboardAsync()
        {
            Action sbConnect = new Action(() =>
            {
                sbSocket = new SocketCommands(sbAddress, sbPort);
                sbSocket.ConnectSocket();
                sbSocket.BeginReceiving(OutputBuffer, new AsyncCallback(ReceivingCallback), this);
                transactionId++;
                sbSocket.SendCommand($"USR {transactionId} {UserInfo.Email} {authString}\r\n");
            });
            await Task.Run(sbConnect);
            Connected = true;
        }

        public void FillMessageHistory()
        {
            if (KeepMessagingHistory)
            {
                List<string> JSONMessages = DatabaseAccess.ReturnMessagesFromSenderAndReceiver(UserInfo.Email, PrincipalInfo.Email);
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

        public async Task InvitePrincipal(string principalEmail)
        {
            if (Connected)
            {
                PrincipalInfo.Email = principalEmail;
                transactionId++;
                await Task.Run(() =>
                {
                    sbSocket.SendCommand($"CAL {transactionId} {principalEmail}\r\n");
                });
            }
            else
            {
                throw new Exceptions.NotConnectedException();
            }
        }

        public async Task InvitePrincipal(string principalEmail, string principalDisplayName)
        {
            if (Connected)
            {
                PrincipalInfo.Email = principalEmail;
                transactionId++;
                await Task.Run(() =>
                {
                    sbSocket.SendCommand($"CAL {transactionId} {principalEmail}\r\n");
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        PrincipalInfo.displayName = principalDisplayName;
                    });
                });
            }
            else
            {
                throw new Exceptions.NotConnectedException();
            }
        }

        public async Task SendTextMessage(string messageText)
        {
            if (Connected && PrincipalsConnected > 0)
            {
                Action messageAction = new Action(() =>
                {
                    string message = "MIME-Version: 1.0\r\nContent-Type: text/plain; charset=UTF-8\r\nX-MMS-IM-Format: FN=Arial; EF=; CO=0; CS=0; PF=22\r\n\r\n" + messageText;
                    byte[] byteMessage = Encoding.UTF8.GetBytes(message);
                    transactionId++;
                    sbSocket.SendCommand($"MSG {transactionId} N {byteMessage.Length}\r\n{message}");
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Message newMessage = new Message()
                        {
                            MessageText = messageText,
                            Sender = UserInfo.displayName,
                            Receiver = PrincipalInfo.displayName,
                            SenderEmail = UserInfo.Email,
                            ReceiverEmail = PrincipalInfo.Email,
                            IsHistory = false
                        };
                        AddToMessageListAndDatabase(newMessage);
                    });
                });
                try
                {
                    await Task.Run(messageAction);
                }
                catch (AggregateException ae)
                {
                    foreach (Exception ie in ae.InnerExceptions)
                    {
                        Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            MessageList.Add(new Message()
                            {
                                MessageText = "There was an error sending this message: " + ie.Message,
                                Sender = "Error",
                                IsHistory = false
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MessageList.Add(new Message()
                        {
                            MessageText = "There was an error sending this message: " + ex.Message,
                            Sender = "Error",
                            IsHistory = false
                        });
                    });
                }
            }
        }

        public async Task SendTypingUser()
        {
            if (Connected && PrincipalsConnected > 0 && !waitingTyping)
            {
                await Task.Run(() =>
                {
                    string message = $"MIME-Version: 1.0\r\nContent-Type: text/x-msmsgscontrol\r\nTypingUser: {UserInfo.Email}\r\n\r\n\r\n";
                    byte[] byteMessage = Encoding.UTF8.GetBytes(message);
                    transactionId++;
                    sbSocket.SendCommand($"MSG {transactionId} U {byteMessage.Length}\r\n{message}");
                });
                waitingTyping = true;
                //sending TypingUser every 5 seconds only
                await Task.Delay(5000);
                waitingTyping = false;
            }
        }

        public async Task SendNudge()
        {
            if (Connected && PrincipalsConnected > 0)
            {
                if (!waitingNudge)
                {
                    try
                    {
                        await Task.Run(() =>
                        {
                            string nudgeMessage = "MIME-Version: 1.0\r\nContent-Type: text/x-msnmsgr-datacast\r\n\r\nID: 1\r\n";
                            byte[] byteMessage = Encoding.UTF8.GetBytes(nudgeMessage);
                            transactionId++;
                            sbSocket.SendCommand($"MSG {transactionId} A {byteMessage.Length}\r\n{nudgeMessage}");
                        });
                        string nudgeText = $"You sent {PrincipalInfo.displayName} a nudge";
                        Message newMessage = new Message()
                        {
                            MessageText = nudgeText,
                            Receiver = PrincipalInfo.displayName,
                            SenderEmail = UserInfo.Email,
                            ReceiverEmail = PrincipalInfo.Email,
                            IsHistory = false
                        };
                        AddToMessageListAndDatabase(newMessage);
                    }
                    catch (Exception ex)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            MessageList.Add(new Message()
                            {
                                MessageText = "There was an error sending this message: " + ex.Message,
                                Sender = "Error",
                                IsHistory = false
                            });
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
                        MessageList.Add(new Message()
                        {
                            MessageText = "Wait before sending nudge again",
                            Sender = "",
                            IsHistory = false
                        });
                    });
                }
            }
        }

        public static string GenerateMessageID()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("{");
            stringBuilder.Append(Guid.NewGuid().ToString().ToUpper());
            stringBuilder.Append("}");
            string message_id = stringBuilder.ToString();
            return message_id;
        }

        private List<InkChunk> DivideInkIntoChunks(byte[] ink_bytes, string MessageId)
        {
            List<InkChunk> InkChunks = new List<InkChunk>();
            double NumberOfChunksDouble = ink_bytes.Length / maximumInkSize;
            NumberOfChunksDouble = Math.Ceiling(NumberOfChunksDouble);
            int NumberOfChunks = Convert.ToInt32(NumberOfChunksDouble);
            int NumberOfFullChunks = NumberOfChunks;
            if (NumberOfChunks % maximumInkSize > 0)
            {
                NumberOfFullChunks--;
            }
            int ink_pos = 0;
            byte[] ink_chunk = new byte[maximumInkSize];
            Buffer.BlockCopy(ink_bytes, ink_pos, ink_chunk, 0, maximumInkSize);
            InkChunks.Add(new InkChunk()
            {
                ChunkNumber = 0,
                MessageID = MessageId,
                EncodedChunk = "base64:" + Convert.ToBase64String(ink_chunk)
            });
            ink_pos += maximumInkSize;
            for (int i = 1; i <= NumberOfFullChunks; i++)
            {
                Buffer.BlockCopy(ink_bytes, ink_pos, ink_chunk, 0, maximumInkSize);
                InkChunks.Add(new InkChunk()
                {
                    ChunkNumber = i,
                    MessageID = MessageId,
                    EncodedChunk = Convert.ToBase64String(ink_chunk)
                });
                ink_pos += maximumInkSize;
            }
            int LastChunkLen = ink_bytes.Length - ink_pos;
            ink_chunk = new byte[LastChunkLen];
            Buffer.BlockCopy(ink_bytes, ink_pos, ink_chunk, 0, LastChunkLen);
            InkChunks.Add(new InkChunk()
            {
                ChunkNumber = NumberOfChunks,
                MessageID = MessageId,
                EncodedChunk = Convert.ToBase64String(ink_chunk)
            });
            return InkChunks;
        }

        public async Task SendInk(byte[] ink_bytes)
        {
            if (ink_bytes.Length > maximumInkSize)
            {
                string MessageId = GenerateMessageID();
                List<InkChunk> InkChunks = DivideInkIntoChunks(ink_bytes, MessageId);
                transactionId++;
                string InkChunkMessagePayload = $"Mime-Version: 1.0\r\nContent-Type: application/x-ms-ink\r\nMessage-ID: {MessageId}\r\nChunks: {InkChunks.Count}\r\n\r\n{InkChunks[0].EncodedChunk}";
                string InkChunkMessage = $"MSG {transactionId} N {Encoding.UTF8.GetBytes(InkChunkMessagePayload).Length}\r\n{InkChunkMessagePayload}";
                try
                {
                    sbSocket.SendCommand(InkChunkMessage);
                }
                catch (Exception ex)
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MessageList.Add(new Message()
                        {
                            MessageText = "There was an error sending this message: " + ex.Message,
                            Sender = "Error",
                            IsHistory = false
                        });
                    });
                }
                for (int i = 1; i < InkChunks.Count; i++)
                {
                    transactionId++;
                    InkChunkMessagePayload = $"Message-ID: {MessageId}\r\nChunk: {InkChunks[i].ChunkNumber}\r\n\r\n{InkChunks[i].EncodedChunk}";
                    InkChunkMessage = $"MSG {transactionId} N {Encoding.UTF8.GetBytes(InkChunkMessagePayload).Length}\r\n{InkChunkMessagePayload}";
                    try
                    {
                        sbSocket.SendCommand(InkChunkMessage);
                    }
                    catch (Exception ex)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            MessageList.Add(new Message()
                            {
                                MessageText = "There was an error sending this message: " + ex.Message,
                                Sender = "Error",
                                IsHistory = false
                            });
                        });
                        return;
                    }
                }
            }
            else
            {
                transactionId++;
                string InkChunkMessagePayload = $"Mime-Version: 1.0\r\nContent-Type: application/x-ms-ink\r\n\r\n{"base64:" + Convert.ToBase64String(ink_bytes)}";
                string InkChunkMessage = $"MSG {transactionId} N {Encoding.UTF8.GetBytes(InkChunkMessagePayload).Length}\r\n{InkChunkMessagePayload}";
                try
                {
                    sbSocket.SendCommand(InkChunkMessage);
                }
                catch (Exception ex)
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MessageList.Add(new Message()
                        {
                            MessageText = "There was an error sending this message: " + ex.Message,
                            Sender = "Error",
                            IsHistory = false
                        });
                    });
                    return;
                }
            }
            Message InkMessage = new Message()
            {
                MessageText = $"You sent {PrincipalInfo.displayName} ink",
                SenderEmail = UserInfo.Email,
                Receiver = PrincipalInfo.displayName,
                ReceiverEmail = PrincipalInfo.Email
            };
            AddToMessageList(InkMessage);
            //sends ink in ISF format
        }

        public async Task AnswerRNG()
        {
            await Task.Run(() =>
            {
                sbSocket = new SocketCommands(sbAddress, sbPort);
                sbSocket.ConnectSocket();
                sbSocket.BeginReceiving(OutputBuffer, new AsyncCallback(ReceivingCallback), this);
                transactionId++;
                sbSocket.SendCommand($"ANS {transactionId} {UserInfo.Email} {authString} {SessionID}\r\n");
            });
        }

        public void Exit()
        {
            sbSocket.SendCommand("OUT\r\n");
            sbSocket.CloseSocket();
        }

        ~SwitchboardConnection()
        {
            Exit();
        }
    }
}
