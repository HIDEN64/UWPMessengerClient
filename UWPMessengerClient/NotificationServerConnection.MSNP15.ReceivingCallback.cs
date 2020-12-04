using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWPMessengerClient
{
    public partial class NotificationServerConnection
    {
        private string SOAPResult;
        private string SSO_ticket;

        public void MSNP15ReceivingCallback(IAsyncResult asyncResult)
        {
            NotificationServerConnection notificationServerConnection = (NotificationServerConnection)asyncResult.AsyncState;
            int bytes_read = notificationServerConnection.NSSocket.StopReceiving(asyncResult);
            notificationServerConnection.output_string = Encoding.UTF8.GetString(notificationServerConnection.received_bytes, 0, bytes_read);
            if (notificationServerConnection.output_string.Contains("MBI_KEY_OLD"))
            {
                GetMBIKeyOldNonce();
                ContinueLoginToMessenger();
            }
            if (bytes_read > 0)
            {
                notificationServerConnection.NSSocket.BeginReceiving(notificationServerConnection.received_bytes, new AsyncCallback(MSNP15ReceivingCallback), notificationServerConnection);
            }
        }

        protected void GetMBIKeyOldNonce()
        {
            string[] USRResponse = output_string.Split("USR ", 2);
            //ensuring the last element of the USRReponse array is just the USR response
            int rnIndex = USRResponse.Last().IndexOf("\r\n");
            if (rnIndex != USRResponse.Last().Length && rnIndex >= 0)
            {
                USRResponse[USRResponse.Length - 1] = USRResponse.Last().Remove(rnIndex);
            }
            string[] USRParams = USRResponse[1].Split(" ");
            string mbi_key_old = USRParams[4];
            MBIKeyOld_nonce = mbi_key_old;
        }

        protected void ContinueLoginToMessenger()
        {
            SOAPResult = PerformSoapSSO();
            string response_struct = GetSSOReturnValue();
            NSSocket.SendCommand($"USR 4 SSO S {SSO_ticket} {response_struct}\r\n");
        }
    }
}
