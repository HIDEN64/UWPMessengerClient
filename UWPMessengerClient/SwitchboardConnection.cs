using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWPMessengerClient
{
    partial class SwitchboardConnection
    {
        private SocketCommands SBSocket;
        private string SBAddress;
        private int SBPort = 0;
        private string userEmail;
        private string TrID;
        public string outputString { get; set; }
        public byte[] outputBuffer { get; set; } = new byte[4096];

        public SwitchboardConnection(string address, int port, string email, string trID)
        {
            SBAddress = address;
            SBPort = port;
            userEmail = email;
            TrID = trID;
        }

        public async Task LoginToNewSwitchboardAsync()
        {
            Action sbconnect = new Action(() =>
            {
                SBSocket = new SocketCommands(SBAddress, SBPort);
                SBSocket.ConnectSocket();
                SBSocket.SendCommand($"USR 1 {userEmail} {TrID}\r\n");
            });
            await Task.Run(sbconnect);
        }
    }
}
