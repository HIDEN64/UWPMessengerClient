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
        protected int TransactionID = 0;
        protected string AuthString;
        public string SessionID { get; protected set; }
        public UserInfo PrincipalInfo { get; set; } = new UserInfo();
        public UserInfo userInfo { get; set; } = new UserInfo();
        public bool Connected { get; set; }
        public int PrincipalsConnected { get; set; }
        public string OutputString { get; set; }
        public byte[] OutputBuffer { get; set; } = new byte[4096];
        public bool KeepMessagingHistory { get; set; } = true;
        protected bool WaitingTyping = false;
        protected bool WaitingNudge = false;
        protected int MaximumInkSize = 1140;
        private static Random random = new Random();
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler HistoryLoaded;
        Dictionary<string, Action> CommandHandlers;
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

        public SwitchboardConnection(string address, int port, string email, string authString, string userDisplayName)
        {
            CommandHandlers = new Dictionary<string, Action>()
            {
                {"USR", () => HandleUSR() },
                {"ANS", () => HandleANS() },
                {"CAL", () => HandleCAL() },
                {"JOI", () => PrincipalsConnected++ },
                {"IRO", () => PrincipalsConnected++ },
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
            CommandHandlers = new Dictionary<string, Action>()
            {
                {"USR", () => HandleUSR() },
                {"ANS", () => HandleANS() },
                {"CAL", () => HandleCAL() },
                {"JOI", () => PrincipalsConnected++ },
                {"IRO", () => PrincipalsConnected++ },
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
            Action sbconnect = new Action(() =>
            {
                SBSocket = new SocketCommands(SBAddress, SBPort);
                SBSocket.ConnectSocket();
                SBSocket.BeginReceiving(OutputBuffer, new AsyncCallback(ReceivingCallback), this);
                TransactionID++;
                SBSocket.SendCommand($"USR {TransactionID} {userInfo.Email} {AuthString}\r\n");
            });
            await Task.Run(sbconnect);
            Connected = true;
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
            if (Connected)
            {
                PrincipalInfo.Email = principal_email;
                TransactionID++;
                await Task.Run(() =>
                {
                    SBSocket.SendCommand($"CAL {TransactionID} {principal_email}\r\n");
                });
            }
            else
            {
                throw new Exceptions.NotConnectedException();
            }
        }

        public async Task InvitePrincipal(string principal_email, string principal_display_name)
        {
            if (Connected)
            {
                PrincipalInfo.Email = principal_email;
                TransactionID++;
                await Task.Run(() =>
                {
                    SBSocket.SendCommand($"CAL {TransactionID} {principal_email}\r\n");
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        PrincipalInfo.displayName = principal_display_name;
                    });
                });
            }
            else
            {
                throw new Exceptions.NotConnectedException();
            }
        }

        public async Task SendTextMessage(string message_text)
        {
            if (Connected && PrincipalsConnected > 0)
            {
                Action message_action = new Action(() =>
                {
                    string message = "MIME-Version: 1.0\r\nContent-Type: text/plain; charset=UTF-8\r\nX-MMS-IM-Format: FN=Arial; EF=; CO=0; CS=0; PF=22\r\n\r\n" + message_text;
                    byte[] byte_message = Encoding.UTF8.GetBytes(message);
                    TransactionID++;
                    SBSocket.SendCommand($"MSG {TransactionID} N {byte_message.Length}\r\n{message}");
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Message newMessage = new Message()
                        {
                            message_text = message_text,
                            sender = userInfo.displayName,
                            receiver = PrincipalInfo.displayName,
                            sender_email = userInfo.Email,
                            receiver_email = PrincipalInfo.Email,
                            IsHistory = false
                        };
                        AddToMessageListAndDatabase(newMessage);
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
                            MessageList.Add(new Message()
                            {
                                message_text = "There was an error sending this message: " + ie.Message,
                                sender = "Error",
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
                            message_text = "There was an error sending this message: " + ex.Message,
                            sender = "Error",
                            IsHistory = false
                        });
                    });
                }
            }
        }

        public async Task SendTypingUser()
        {
            if (Connected && PrincipalsConnected > 0 && !WaitingTyping)
            {
                await Task.Run(() =>
                {
                    string message = $"MIME-Version: 1.0\r\nContent-Type: text/x-msmsgscontrol\r\nTypingUser: {userInfo.Email}\r\n\r\n\r\n";
                    byte[] byte_message = Encoding.UTF8.GetBytes(message);
                    TransactionID++;
                    SBSocket.SendCommand($"MSG {TransactionID} U {byte_message.Length}\r\n{message}");
                });
                WaitingTyping = true;
                //sending TypingUser every 5 seconds only
                await Task.Delay(5000);
                WaitingTyping = false;
            }
        }

        public async Task SendNudge()
        {
            if (Connected && PrincipalsConnected > 0)
            {
                if (!WaitingNudge)
                {
                    try
                    {
                        await Task.Run(() =>
                        {
                            string nudge_message = "MIME-Version: 1.0\r\nContent-Type: text/x-msnmsgr-datacast\r\n\r\nID: 1\r\n";
                            byte[] byte_message = Encoding.UTF8.GetBytes(nudge_message);
                            TransactionID++;
                            SBSocket.SendCommand($"MSG {TransactionID} A {byte_message.Length}\r\n{nudge_message}");
                        });
                        string nudge_text = $"You sent {PrincipalInfo.displayName} a nudge";
                        Message newMessage = new Message()
                        {
                            message_text = nudge_text,
                            receiver = PrincipalInfo.displayName,
                            sender_email = userInfo.Email,
                            receiver_email = PrincipalInfo.Email,
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
                                message_text = "There was an error sending this message: " + ex.Message,
                                sender = "Error",
                                IsHistory = false
                            });
                        });
                    }
                    WaitingNudge = true;
                    //12 second cooldown, about the same as the wlm 2009 client
                    await Task.Delay(12000);
                    WaitingNudge = false;
                }
                else
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MessageList.Add(new Message()
                        {
                            message_text = "Wait before sending nudge again",
                            sender = "",
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
            InkChunks.Add(new InkChunk()
            {
                ChunkNumber = 0,
                MessageID = MessageId,
                EncodedChunk = "base64:" + Convert.ToBase64String(ink_chunk)
            });
            ink_pos += MaximumInkSize;
            for (int i = 1; i <= NumberOfFullChunks; i++)
            {
                Buffer.BlockCopy(ink_bytes, ink_pos, ink_chunk, 0, MaximumInkSize);
                InkChunks.Add(new InkChunk()
                {
                    ChunkNumber = i,
                    MessageID = MessageId,
                    EncodedChunk = Convert.ToBase64String(ink_chunk)
                });
                ink_pos += MaximumInkSize;
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
            if (ink_bytes.Length > MaximumInkSize)
            {
                string MessageId = GenerateMessageID();
                List<InkChunk> InkChunks = DivideInkIntoChunks(ink_bytes, MessageId);
                TransactionID++;
                string InkChunkMessagePayload = $"Mime-Version: 1.0\r\nContent-Type: application/x-ms-ink\r\nMessage-ID: {MessageId}\r\nChunks: {InkChunks.Count}\r\n\r\n{InkChunks[0].EncodedChunk}";
                string InkChunkMessage = $"MSG {TransactionID} N {Encoding.UTF8.GetBytes(InkChunkMessagePayload).Length}\r\n{InkChunkMessagePayload}";
                try
                {
                    SBSocket.SendCommand(InkChunkMessage);
                }
                catch (Exception ex)
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MessageList.Add(new Message()
                        {
                            message_text = "There was an error sending this message: " + ex.Message,
                            sender = "Error",
                            IsHistory = false
                        });
                    });
                }
                for (int i = 1; i < InkChunks.Count; i++)
                {
                    TransactionID++;
                    InkChunkMessagePayload = $"Message-ID: {MessageId}\r\nChunk: {InkChunks[i].ChunkNumber}\r\n\r\n{InkChunks[i].EncodedChunk}";
                    InkChunkMessage = $"MSG {TransactionID} N {Encoding.UTF8.GetBytes(InkChunkMessagePayload).Length}\r\n{InkChunkMessagePayload}";
                    try
                    {
                        SBSocket.SendCommand(InkChunkMessage);
                    }
                    catch (Exception ex)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            MessageList.Add(new Message()
                            {
                                message_text = "There was an error sending this message: " + ex.Message,
                                sender = "Error",
                                IsHistory = false
                            });
                        });
                        return;
                    }
                }
            }
            else
            {
                TransactionID++;
                string InkChunkMessagePayload = $"Mime-Version: 1.0\r\nContent-Type: application/x-ms-ink\r\n\r\n{"base64:" + Convert.ToBase64String(ink_bytes)}";
                string InkChunkMessage = $"MSG {TransactionID} N {Encoding.UTF8.GetBytes(InkChunkMessagePayload).Length}\r\n{InkChunkMessagePayload}";
                try
                {
                    SBSocket.SendCommand(InkChunkMessage);
                }
                catch (Exception ex)
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MessageList.Add(new Message()
                        {
                            message_text = "There was an error sending this message: " + ex.Message,
                            sender = "Error",
                            IsHistory = false
                        });
                    });
                    return;
                }
            }
            Message InkMessage = new Message()
            {
                message_text = $"You sent {PrincipalInfo.displayName} ink",
                sender_email = userInfo.Email,
                receiver = PrincipalInfo.displayName,
                receiver_email = PrincipalInfo.Email
            };
            AddToMessageList(InkMessage);
            //sends ink in ISF format
        }

        public async Task AnswerRNG()
        {
            await Task.Run(() =>
            {
                SBSocket = new SocketCommands(SBAddress, SBPort);
                SBSocket.ConnectSocket();
                SBSocket.BeginReceiving(OutputBuffer, new AsyncCallback(ReceivingCallback), this);
                TransactionID++;
                SBSocket.SendCommand($"ANS {TransactionID} {userInfo.Email} {AuthString} {SessionID}\r\n");
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
