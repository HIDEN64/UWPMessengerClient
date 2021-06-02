using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Text;
using System.Threading.Tasks;

namespace UWPMessengerClient.MSNP
{
    public partial class NotificationServerConnection
    {
        private HttpClient httpClient;
        private string token;

        private async Task MSNP12LoginToMessengerAsync()
        {
            httpClient = new HttpClient();
            nsSocket = new SocketCommands(NsAddress, port);
            Action loginAction = new Action(() =>
            {
                //sequence of commands to login to escargot
                nsSocket.ConnectSocket();
                nsSocket.SetReceiveTimeout(25000);
                UserInfo.Email = email;
                GetContactsFromDatabase();
                //begin receiving from escargot
                nsSocket.BeginReceiving(receivedBytes, new AsyncCallback(ReceivingCallback), this);
                TransactionId++;
                nsSocket.SendCommand($"VER {TransactionId} MSNP12 CVR0\r\n");//send msnp version
                TransactionId++;
                nsSocket.SendCommand($"CVR {TransactionId} 0x0409 winnt 10 i386 UWPMESSENGER 0.6 msmsgs\r\n");//send client information
                TransactionId++;
                nsSocket.SendCommand($"USR {TransactionId} TWN I {email}\r\n");//sends email to get a string for use in authentication
                TransactionId++;
                Task<string> tokenTask = GetNexusTokenAsync(httpClient);
                token = tokenTask.Result;
                nsSocket.SendCommand($"USR {TransactionId} TWN S t={token}\r\n");//sending authentication token
                TransactionId++;
                nsSocket.SendCommand($"SYN {TransactionId} 0 0\r\n");//sync contact list
                TransactionId++;
                nsSocket.SendCommand($"CHG {TransactionId} {UserPresenceStatus} {ClientCapabilities}\r\n");//setting selected presence status
            });
            await Task.Run(loginAction);
        }

        public async Task<string> GetNexusTokenAsync(HttpClient httpClient)
        {
            //makes a request to the nexus and gets the Www-Authenticate header
            HttpResponseMessage response = await httpClient.GetAsync(NexusAddress);
            response.EnsureSuccessStatusCode();
            HttpResponseHeaders responseHeaders = response.Headers;
            //parsing the response headers to extract the login server adress
            string headersString = responseHeaders.ToString();
            string[] splitHeadersString = headersString.Split("DALogin=");
            string DALogin = splitHeadersString[1];
            DALogin = DALogin.Remove(DALogin.IndexOf("\r"));
            if (UsingLocalhost)
            {
                DALogin = "http://localhost/login";
            }
            string emailEncoded = HttpUtility.UrlEncode(email);
            string passwordEncoded = HttpUtility.UrlEncode(password);
            //makes a request to the login address and gets the from-PP header
            httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Passport1.4 OrgVerb=GET,OrgUrl=http%3A%2F%2Fmessenger%2Emsn%2Ecom,sign-in={emailEncoded},pwd={passwordEncoded},ct=1,rver=1,wp=FS_40SEC_0_COMPACT,lc=1,id=1");
            response = await httpClient.GetAsync(DALogin);
            response.EnsureSuccessStatusCode();
            responseHeaders = response.Headers;
            //parsing the response headers to extract the token
            headersString = responseHeaders.ToString();
            string[] fromPpSplit = headersString.Split("from-PP='");
            string fromPP = fromPpSplit[1];
            fromPP = fromPP.Remove(fromPP.IndexOf("'\r"));
            return fromPP;
        }
    }
}
