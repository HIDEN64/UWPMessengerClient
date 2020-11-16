using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace UWPMessengerClient
{
    class NotificationServerConnection
    {
        private readonly string nexus_address = "https://m1.escargot.log1p.xyz/nexus-mock";
        //notification server(escargot) address
        private readonly string NSaddress = "m1.escargot.log1p.xyz";
        private readonly int port = 1863;
        private string email;
        private string password;
        private string token;
        string[] output_buffer_array;

        public NotificationServerConnection(string escargot_email, string escargot_password)
        {
            email = escargot_email;
            password = escargot_password;
        }

        public async Task<string[]> login_to_messengerAsync()
        {
            HttpClient httpClient = new HttpClient();
            SocketCommands NSSocket = new SocketCommands(NSaddress, port);
            await Task.Run(() =>
            {
                /*sequence of commands to login to escargot, sends them then reads the 
                response and stores it in the output buffer*/
                output_buffer_array = new string[11];
                int currentIndex = 0;
                NSSocket.NSConnectSocket();
                NSSocket.SendCommand("VER 1 MSNP12 CVR0\r\n");
                output_buffer_array[currentIndex] = NSSocket.ReceiveMessage();
                NSSocket.SendCommand("CVR 2 0x0409 winnt 10 i386 UWPMESSENGER 0.1 msmsgs\r\n");
                currentIndex++;
                output_buffer_array[currentIndex] = NSSocket.ReceiveMessage();
                NSSocket.SendCommand($"USR 3 TWN I {email}\r\n");
                currentIndex++;
                output_buffer_array[currentIndex] = NSSocket.ReceiveMessage();
                Task<string> token_task = getNexusTokenAsync(httpClient);
                token = token_task.Result;
                NSSocket.SendCommand($"USR 4 TWN S t={token}\r\n");
                currentIndex++;
                output_buffer_array[currentIndex] = NSSocket.ReceiveMessage();
                NSSocket.SendCommand("SYN 5 0 0\r\n");
                currentIndex++;
                output_buffer_array[currentIndex] = NSSocket.ReceiveMessage();
                currentIndex++;
                output_buffer_array[currentIndex] = NSSocket.ReceiveMessage();
                currentIndex++;
                output_buffer_array[currentIndex] = NSSocket.ReceiveMessage();
                NSSocket.SendCommand("CHG 6 NLN 0\r\n");
                currentIndex++;
                output_buffer_array[currentIndex] = NSSocket.ReceiveMessage();
                currentIndex++;
                output_buffer_array[currentIndex] = NSSocket.ReceiveMessage();
                currentIndex++;
                output_buffer_array[currentIndex] = NSSocket.ReceiveMessage();
                for (int i = 0;i<output_buffer_array.Length; ++i)
                {
                    try
                    {
                        output_buffer_array[i] = output_buffer_array[i].Replace("\0", "");
                    }
                    catch (NullReferenceException)
                    {
                        output_buffer_array[i] = "";
                    }
                }
            });
            return output_buffer_array;
        }

        public async Task<string> getNexusTokenAsync(HttpClient httpClient)
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
