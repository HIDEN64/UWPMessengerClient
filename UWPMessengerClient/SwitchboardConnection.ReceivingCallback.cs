using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWPMessengerClient
{
    partial class SwitchboardConnection
    {
        public void ReceivingCallback(IAsyncResult asyncResult)
        {
            SwitchboardConnection switchboardConnection = (SwitchboardConnection)asyncResult.AsyncState;
            int bytes_received = switchboardConnection.SBSocket.StopReceiving(asyncResult);
            switchboardConnection.outputString = Encoding.ASCII.GetString(switchboardConnection.outputBuffer, 0, bytes_received);
            if (bytes_received > 0)
            {
                SBSocket.BeginReceiving(outputBuffer, new AsyncCallback(ReceivingCallback), switchboardConnection);
            }
        }
    }
}
