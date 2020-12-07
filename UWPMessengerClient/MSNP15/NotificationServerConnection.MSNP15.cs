using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml;
using System.IO;
using System.Security.Cryptography;
using Windows.UI.Core;

namespace UWPMessengerClient.MSNP15
{
    public partial class NotificationServerConnection
    {
        private SocketCommands NSSocket;
        public SwitchboardConnection SBConnection { get; set; }
        private readonly string NSaddress = "m1.escargot.log1p.xyz";
        private readonly string RST_address = "https://m1.escargot.log1p.xyz/RST.srf";
        //private readonly string RST_address = "http://localhost/RST.srf";
        private readonly int port = 1863;
        private string email;
        private string password;
        public int ContactIndexToChat { get; set; }
        public string CurrentUserPresenceStatus { get; set; }

        public NotificationServerConnection(string escargot_email, string escargot_password)
        {
            email = escargot_email;
            password = escargot_password;
        }

        public static HttpWebRequest CreateSOAPRequest(string soap_action, string address)
        {
            HttpWebRequest request = WebRequest.CreateHttp(address);
            request.Headers.Add($@"SOAPAction:{soap_action}");
            request.ContentType = "text/xml;charset=\"utf-8\"";
            request.Accept = "text/xml";
            request.Method = "POST";
            return request;
        }

        public string MakeSOAPRequest(string SOAP_body, string address, string soap_action)
        {
            HttpWebRequest SOAPRequest = CreateSOAPRequest(soap_action, address);
            XmlDocument SoapXMLBody = new XmlDocument();
            SoapXMLBody.LoadXml(SOAP_body);
            using (Stream stream = SOAPRequest.GetRequestStream())
            {
                SoapXMLBody.Save(stream);
            }
            using (WebResponse webResponse = SOAPRequest.GetResponse())
            {
                using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                {
                    var result = rd.ReadToEnd();
                    return result;
                }
            }
        }

        public static byte[] JoinBytes(byte[] first, byte[] second)
        {
            return first.Concat(second).ToArray();
        }

        public void FillForwardListCollection()
        {
            foreach (Contact contact in contact_list)
            {
                if (contact.onForward == true)
                {
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        contacts_in_forward_list.Add(contact);
                    });
                }
            }
        }

        public async Task ChangePresence(string status)
        {
            throw new NotImplementedException();
        }

        public async Task ChangeUserDisplayName(string newDisplayName)
        {
            throw new NotImplementedException();
        }

        public async Task InitiateSB()
        {
            throw new NotImplementedException();
        }

        public void Exit()
        {
            NSSocket.SendCommand("OUT\r\n");
            NSSocket.CloseSocket();
        }

        ~NotificationServerConnection()
        {
            Exit();
        }
    }
}
