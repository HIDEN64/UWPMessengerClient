using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWPMessengerClient.MSNP;

namespace UWPMessengerClient
{
    class ChatPageNavigationParams
    {
        public NotificationServerConnection notificationServerConnection;
        public string SessionID;
        public bool ExistingSwitchboard;
    }
}
