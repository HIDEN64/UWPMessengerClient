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
        private SocketCommands sbSocket;
        public string SbAddress { get; private set; }
        public int SbPort { get; private set; } = 1864;
        public int TransactionId { get; private set; } = 0;
        private string authString;
        public string SessionID { get; private set; }
        public UserInfo PrincipalInfo { get; set; } = new UserInfo();
        public UserInfo UserInfo { get; set; } = new UserInfo();
        public bool Connected { get; set; }
        public int PrincipalsConnected { get; set; }
        public string OutputString { get; set; }
        public byte[] OutputBuffer { get; set; } = new byte[4096];
        public bool KeepMessagingHistory { get; set; } = true;
        private bool waitingTyping = false;
        private bool waitingNudge = false;
        private int maximumInkSize = 1140;
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler HistoryLoaded;
        private Dictionary<string, Action> commandHandlers;
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
                {"USR", () => HandleUsr() },
                {"ANS", () => HandleAns() },
                {"CAL", () => HandleCal() },
                {"JOI", () => PrincipalsConnected++ },
                {"IRO", () => PrincipalsConnected++ },
                {"MSG", () => HandleMsg() }
            };
            SbAddress = address;
            SbPort = port;
            UserInfo.Email = email;
            this.authString = authString;
            UserInfo.DisplayName = userDisplayName;
        }

        public SwitchboardConnection(string address, int port, string email, string authString, string userDisplayName, string principalDisplayName, string principalEmail, string sessionID)
        {
            commandHandlers = new Dictionary<string, Action>()
            {
                {"USR", () => HandleUsr() },
                {"ANS", () => HandleAns() },
                {"CAL", () => HandleCal() },
                {"JOI", () => PrincipalsConnected++ },
                {"IRO", () => PrincipalsConnected++ },
                {"MSG", () => HandleMsg() }
            };
            SbAddress = address;
            SbPort = port;
            UserInfo.Email = email;
            this.authString = authString;
            SessionID = sessionID;
            UserInfo.DisplayName = userDisplayName;
            PrincipalInfo.DisplayName = principalDisplayName;
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
                sbSocket = new SocketCommands(SbAddress, SbPort);
                sbSocket.ConnectSocket();
                sbSocket.BeginReceiving(OutputBuffer, new AsyncCallback(ReceivingCallback), this);
                TransactionId++;
                sbSocket.SendCommand($"USR {TransactionId} {UserInfo.Email} {authString}\r\n");
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
                TransactionId++;
                await Task.Run(() =>
                {
                    sbSocket.SendCommand($"CAL {TransactionId} {principalEmail}\r\n");
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
                TransactionId++;
                await Task.Run(() =>
                {
                    sbSocket.SendCommand($"CAL {TransactionId} {principalEmail}\r\n");
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        PrincipalInfo.DisplayName = principalDisplayName;
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
                    TransactionId++;
                    sbSocket.SendCommand($"MSG {TransactionId} N {byteMessage.Length}\r\n{message}");
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Message newMessage = new Message()
                        {
                            MessageText = messageText,
                            Sender = UserInfo.DisplayName,
                            Receiver = PrincipalInfo.DisplayName,
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
                    TransactionId++;
                    sbSocket.SendCommand($"MSG {TransactionId} U {byteMessage.Length}\r\n{message}");
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
                            TransactionId++;
                            sbSocket.SendCommand($"MSG {TransactionId} A {byteMessage.Length}\r\n{nudgeMessage}");
                        });
                        string nudgeText = $"You sent {PrincipalInfo.DisplayName} a nudge";
                        Message newMessage = new Message()
                        {
                            MessageText = nudgeText,
                            Receiver = PrincipalInfo.DisplayName,
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
            string messageId = stringBuilder.ToString();
            return messageId;
        }

        private List<InkChunk> DivideInkIntoChunks(byte[] inkBytes, string messageId)
        {
            List<InkChunk> inkChunks = new List<InkChunk>();
            double numberOfChunksDouble = inkBytes.Length / maximumInkSize;
            numberOfChunksDouble = Math.Ceiling(numberOfChunksDouble);
            int numberOfChunks = Convert.ToInt32(numberOfChunksDouble);
            int numberOfFullChunks = numberOfChunks;
            if (inkBytes.Length % maximumInkSize > 0)
            {
                numberOfFullChunks--;
            }
            int inkPos = 0;
            byte[] inkChunk = new byte[maximumInkSize];
            Buffer.BlockCopy(inkBytes, inkPos, inkChunk, 0, maximumInkSize);
            inkChunks.Add(new InkChunk()
            {
                ChunkNumber = 0,
                MessageID = messageId,
                EncodedChunk = "base64:" + Convert.ToBase64String(inkChunk)
            });
            inkPos += maximumInkSize;
            for (int i = 1; i <= numberOfFullChunks; i++)
            {
                Buffer.BlockCopy(inkBytes, inkPos, inkChunk, 0, maximumInkSize);
                inkChunks.Add(new InkChunk()
                {
                    ChunkNumber = i,
                    MessageID = messageId,
                    EncodedChunk = Convert.ToBase64String(inkChunk)
                });
                inkPos += maximumInkSize;
            }
            int lastChunkLength = inkBytes.Length - inkPos;
            inkChunk = new byte[lastChunkLength];
            Buffer.BlockCopy(inkBytes, inkPos, inkChunk, 0, lastChunkLength);
            inkChunks.Add(new InkChunk()
            {
                ChunkNumber = numberOfChunks,
                MessageID = messageId,
                EncodedChunk = Convert.ToBase64String(inkChunk)
            });
            return inkChunks;
        }

        public async Task SendInk(byte[] inkBytes)
        {
            if (inkBytes.Length > maximumInkSize)
            {
                string messageId = GenerateMessageID();
                List<InkChunk> inkChunks = DivideInkIntoChunks(inkBytes, messageId);
                TransactionId++;
                string inkChunkMessagePayload = $"Mime-Version: 1.0\r\nContent-Type: application/x-ms-ink\r\nMessage-ID: {messageId}\r\nChunks: {inkChunks.Count}\r\n\r\n{inkChunks[0].EncodedChunk}";
                string inkChunkMessage = $"MSG {TransactionId} N {Encoding.UTF8.GetBytes(inkChunkMessagePayload).Length}\r\n{inkChunkMessagePayload}";
                try
                {
                    sbSocket.SendCommand(inkChunkMessage);
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
                for (int i = 1; i < inkChunks.Count; i++)
                {
                    TransactionId++;
                    inkChunkMessagePayload = $"Message-ID: {messageId}\r\nChunk: {inkChunks[i].ChunkNumber}\r\n\r\n{inkChunks[i].EncodedChunk}";
                    inkChunkMessage = $"MSG {TransactionId} N {Encoding.UTF8.GetBytes(inkChunkMessagePayload).Length}\r\n{inkChunkMessagePayload}";
                    try
                    {
                        sbSocket.SendCommand(inkChunkMessage);
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
                TransactionId++;
                string inkChunkMessagePayload = $"Mime-Version: 1.0\r\nContent-Type: application/x-ms-ink\r\n\r\n{"base64:" + Convert.ToBase64String(inkBytes)}";
                string inkChunkMessage = $"MSG {TransactionId} N {Encoding.UTF8.GetBytes(inkChunkMessagePayload).Length}\r\n{inkChunkMessagePayload}";
                try
                {
                    sbSocket.SendCommand(inkChunkMessage);
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
            Message inkMessage = new Message()
            {
                MessageText = $"You sent {PrincipalInfo.DisplayName} ink",
                SenderEmail = UserInfo.Email,
                Receiver = PrincipalInfo.DisplayName,
                ReceiverEmail = PrincipalInfo.Email
            };
            AddToMessageList(inkMessage);
            //sends ink in ISF format
        }

        public async Task AnswerRNG()
        {
            await Task.Run(() =>
            {
                sbSocket = new SocketCommands(SbAddress, SbPort);
                sbSocket.ConnectSocket();
                sbSocket.BeginReceiving(OutputBuffer, new AsyncCallback(ReceivingCallback), this);
                TransactionId++;
                sbSocket.SendCommand($"ANS {TransactionId} {UserInfo.Email} {authString} {SessionID}\r\n");
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
