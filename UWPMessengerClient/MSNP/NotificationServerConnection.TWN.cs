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
            nsSocket = new SocketCommands(nsAddress, port);
            Action loginAction = new Action(() =>
            {
                //sequence of commands to login to escargot
                nsSocket.ConnectSocket();
                nsSocket.SetReceiveTimeout(25000);
                UserInfo.Email = email;
                GetContactsFromDatabase();
                //begin receiving from escargot
                nsSocket.BeginReceiving(receivedBytes, new AsyncCallback(ReceivingCallback), this);
                transactionId++;
                nsSocket.SendCommand($"VER {transactionId} MSNP12 CVR0\r\n");//send msnp version
                transactionId++;
                nsSocket.SendCommand($"CVR {transactionId} 0x0409 winnt 10 i386 UWPMESSENGER 0.6 msmsgs\r\n");//send client information
                transactionId++;
                nsSocket.SendCommand($"USR {transactionId} TWN I {email}\r\n");//sends email to get a string for use in authentication
                transactionId++;
                Task<string> token_task = GetNexusTokenAsync(httpClient);
                token = token_task.Result;
                nsSocket.SendCommand($"USR {transactionId} TWN S t={token}\r\n");//sending authentication token
                transactionId++;
                nsSocket.SendCommand($"SYN {transactionId} 0 0\r\n");//sync contact list
                transactionId++;
                nsSocket.SendCommand($"CHG {transactionId} {UserPresenceStatus} {clientCapabilities}\r\n");//set presence as available
            });
            await Task.Run(loginAction);
        }

        public async Task<string> GetNexusTokenAsync(HttpClient httpClient)
        {
            //makes a request to the nexus and gets the Www-Authenticate header
            HttpResponseMessage response = await httpClient.GetAsync(nexusAddress);
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
