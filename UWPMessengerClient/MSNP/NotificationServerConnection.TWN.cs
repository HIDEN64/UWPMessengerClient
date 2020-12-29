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

        protected async Task MSNP12LoginToMessengerAsync()
        {
            httpClient = new HttpClient();
            NSSocket = new SocketCommands(NSaddress, port);
            Action loginAction = new Action(() =>
            {
                //sequence of commands to login to escargot
                NSSocket.ConnectSocket();
                NSSocket.SetReceiveTimeout(15000);
                //begin receiving from escargot
                NSSocket.BeginReceiving(received_bytes, new AsyncCallback(ReceivingCallback), this);
                transactionID++;
                NSSocket.SendCommand($"VER {transactionID} MSNP12 CVR0\r\n");//send msnp version
                transactionID++;
                NSSocket.SendCommand($"CVR {transactionID} 0x0409 winnt 10 i386 UWPMESSENGER 0.6 msmsgs\r\n");//send client information
                transactionID++;
                NSSocket.SendCommand($"USR {transactionID} TWN I {email}\r\n");//sends email to get a string for use in authentication
                transactionID++;
                userInfo.Email = email;
                Task<string> token_task = GetNexusTokenAsync(httpClient);
                token = token_task.Result;
                NSSocket.SendCommand($"USR {transactionID} TWN S t={token}\r\n");//sending authentication token
                transactionID++;
                NSSocket.SendCommand($"SYN {transactionID} 0 0\r\n");//sync contact list
                transactionID++;
                NSSocket.SendCommand($"CHG {transactionID} {UserPresenceStatus} {clientCapabilities}\r\n");//set presence as available
            });
            await Task.Run(loginAction);
        }

        public async Task<string> GetNexusTokenAsync(HttpClient httpClient)
        {
            //makes a request to the nexus and gets the Www-Authenticate header
            HttpResponseMessage response = await httpClient.GetAsync(nexus_address);
            response.EnsureSuccessStatusCode();
            HttpResponseHeaders responseHeaders = response.Headers;
            //parsing the response headers to extract the login server adress
            string headersString = responseHeaders.ToString();
            string[] SplitHeadersString = headersString.Split("DALogin=");
            string DALogin = SplitHeadersString[1];
            DALogin = DALogin.Remove(DALogin.IndexOf("\r"));
            if (_UsingLocalhost)
            {
                DALogin = "http://localhost/login";
            }
            string email_encoded = HttpUtility.UrlEncode(email);
            string password_encoded = HttpUtility.UrlEncode(password);
            //makes a request to the login address and gets the from-PP header
            httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Passport1.4 OrgVerb=GET,OrgUrl=http%3A%2F%2Fmessenger%2Emsn%2Ecom,sign-in={email_encoded},pwd={password_encoded},ct=1,rver=1,wp=FS_40SEC_0_COMPACT,lc=1,id=1");
            response = await httpClient.GetAsync(DALogin);
            response.EnsureSuccessStatusCode();
            responseHeaders = response.Headers;
            //parsing the response headers to extract the token
            headersString = responseHeaders.ToString();
            string[] fromPP_split = headersString.Split("from-PP='");
            string fromPP = fromPP_split[1];
            fromPP = fromPP.Remove(fromPP.IndexOf("'\r"));
            return fromPP;
        }
    }
}
