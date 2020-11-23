using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace UWPMessengerClient
{
    public partial class SwitchboardConnection
    {
        private SocketCommands SBSocket;
        private string SBAddress;
        private int SBPort = 0;
        private string UserEmail;
        private string UserDisplayName;
        private string TrID;
        public bool connected { get; set; }
        public int principalsConnected { get; set; }
        public string outputString { get; set; }
        public byte[] outputBuffer { get; set; } = new byte[4096];

        public SwitchboardConnection(string email, string userDisplayName)
        {
            UserEmail = email;
            UserDisplayName = userDisplayName;
        }

        public SwitchboardConnection(string address, int port, string email, string trID, string userDisplayName)
        {
            SBAddress = address;
            SBPort = port;
            UserEmail = email;
            TrID = trID;
            UserDisplayName = userDisplayName;
        }

        public void SetAddressPortAndTrID(string address, int port, string trID)
        {
            SBAddress = address;
            SBPort = port;
            TrID = trID;
        }

        public async Task LoginToNewSwitchboardAsync()
        {
            Action sbconnect = new Action(() =>
            {
                SBSocket = new SocketCommands(SBAddress, SBPort);
                SBSocket.ConnectSocket();
                SBSocket.BeginReceiving(outputBuffer, new AsyncCallback(ReceivingCallback), this);
                SBSocket.SendCommand($"USR 1 {UserEmail} {TrID}\r\n");
            });
            await Task.Run(sbconnect);
            connected = true;
        }

        public async Task InvitePrincipal(string principal_email)
        {
            if (connected == true)
            {
                await Task.Run(() =>
                {
                    SBSocket.SendCommand($"CAL 2 {principal_email}\r\n");
                    principalsConnected++;
                });
            }
            else
            {
                throw new Exception();
            }
        }

        public async Task SendMessage(string message_text)
        {
            if (connected == true && principalsConnected > 0)
            {
                await Task.Run(() => 
                {
                    string message = "MIME-Version: 1.0\r\nContent-Type: text/plain; charset=UTF-8\r\nX-MMS-IM-Format: FN=Arial; EF=; CO=0; CS=0; PF=22\r\n\r\n" + message_text;
                    int msg_length = message.Length;
                    SBSocket.SendCommand($"MSG 3 N {msg_length}\r\n{message}");
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MessageList.Add(new Message() { message_text = message_text, sender = UserDisplayName});
                    });
                });
            }
        }

        ~SwitchboardConnection()
        {
            SBSocket.SendCommand("OUT\r\n");
            SBSocket.CloseSocket();
        }
    }
}
