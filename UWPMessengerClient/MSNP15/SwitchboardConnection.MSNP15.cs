using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWPMessengerClient.MSNP15
{
    public partial class SwitchboardConnection
    {
        private SocketCommands SBSocket;
        private string SBAddress;
        private int SBPort = 0;
        private string UserEmail;
        private string TrID;
        private string SessionID;
        public UserInfo PrincipalInfo { get; set; } = new UserInfo();
        public UserInfo userInfo { get; set; } = new UserInfo();
        public bool connected { get; set; }
        public int principalsConnected { get; set; }
        public string outputString { get; set; }
        public byte[] outputBuffer { get; set; } = new byte[4096];

        public SwitchboardConnection(string email, string userDisplayName)
        {
            UserEmail = email;
            userInfo.displayName = userDisplayName;
        }

        public SwitchboardConnection(string address, int port, string email, string trID, string userDisplayName)
        {
            SBAddress = address;
            SBPort = port;
            UserEmail = email;
            TrID = trID;
            userInfo.displayName = userDisplayName;
        }

        public SwitchboardConnection(string address, int port, string email, string trID, string userDisplayName, string principalDisplayName, string sessionID)
        {
            SBAddress = address;
            SBPort = port;
            UserEmail = email;
            TrID = trID;
            SessionID = sessionID;
            userInfo.displayName = userDisplayName;
            PrincipalInfo.displayName = principalDisplayName;
        }

        public async Task SendMessage(string message_text)
        {
            throw new NotImplementedException();
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
