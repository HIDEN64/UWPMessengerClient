using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Windows.UI.Core;
using System.Collections.ObjectModel;

namespace UWPMessengerClient
{
    partial class NotificationServerConnection
    {
        private SocketCommands NSSocket;
        private HttpClient httpClient;
        //notification server(escargot) address and nexus address
        private readonly string NSaddress = "m1.escargot.log1p.xyz";
        private readonly string nexus_address = "https://m1.escargot.log1p.xyz/nexus-mock";
        //uncomment below and comment above to use localserver
        //private readonly string NSaddress = "127.0.0.1";
        //private readonly string nexus_address = "http://localhost/nexus-mock";
        private readonly int port = 1863;
        private string email;
        private string password;
        private string token;

        public NotificationServerConnection(string escargot_email, string escargot_password)
        {
            email = escargot_email;
            password = escargot_password;
        }

        public async Task login_to_messengerAsync()
        {
            httpClient = new HttpClient();
            NSSocket = new SocketCommands(NSaddress, port);
            Action loginAction = new Action(() =>
            {
                //sequence of commands to login to escargot
                NSSocket.NSConnectSocket();
                //begin receiving from escargot
                NSSocket.BeginReceiving(received_bytes, new AsyncCallback(ReceivingCallback), this);
                NSSocket.SendCommand("VER 1 MSNP12 CVR0\r\n");//send msnp version
                NSSocket.SendCommand("CVR 2 0x0409 winnt 10 i386 UWPMESSENGER 0.3 msmsgs\r\n");//send client information
                NSSocket.SendCommand($"USR 3 TWN I {email}\r\n");//sends email to get a string for use in authentication
                Task<string> token_task = GetNexusTokenAsync(httpClient);
                token = token_task.Result;
                NSSocket.SendCommand($"USR 4 TWN S t={token}\r\n");//sending authentication token
                NSSocket.SendCommand("SYN 5 0 0\r\n");//sync contact list
                NSSocket.SendCommand("CHG 6 NLN 0\r\n");//set presence as available
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
            //to use local nexus server uncomment
            //DALogin = "http://localhost/login";
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

        public async Task ChangePresence(string status)
        {
            Action changePresence = new Action(() =>
            {
                NSSocket.SendCommand($"CHG 7 {status} 0\r\n");
            });
            await Task.Run(changePresence);
        }
    }
}
