using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml;
using System.IO;
using System.Security.Cryptography;

namespace UWPMessengerClient
{
    public partial class NotificationServerConnection
    {
        private string MBIKeyOld;
        private struct MSGUserKey
        {
            //header
            uint uStructHeaderSize;
            uint uCryptMode;
            uint uCipherMode;
            uint uHashType;
            uint uIVLen;
            uint uHashLen;
            uint uCipherLen;
            //data
            byte[] aIVBytes;
            byte[] aHashBytes;
            byte[] aCipherBytes;
        }

        public async Task StartLoginToMessengerMSNP15Async()
        {
            NSSocket = new SocketCommands(NSaddress, port);
            Action loginAction = new Action(() =>
            {
                NSSocket.ConnectSocket();
                NSSocket.BeginReceiving(received_bytes, new AsyncCallback(MSNP15ReceivingCallback), this);
                NSSocket.SendCommand("VER 1 MSNP15 CVR0\r\n");
                NSSocket.SendCommand("CVR 2 0x0409 winnt 10 i386 UWPMESSENGER 0.4 msmsgs\r\n");
                NSSocket.SendCommand($"USR 3 SSO I {email}\r\n");
            });
            await Task.Run(loginAction);
        }

        public HttpWebRequest CreateSOAPRequest(string soap_action, string address)
        {
            HttpWebRequest request = WebRequest.CreateHttp(address);
            request.Headers.Add($@"SOAPAction:{soap_action}");
            request.ContentType = "text/xml;charset=\"utf-8\"";
            request.Accept = "text/xml";
            request.Method = "POST";
            return request;
        }

        public string PerformSoapSSO()
        {
            HttpWebRequest SOAPRequest = CreateSOAPRequest("http://www.msn.com/webservices/storage/w10/", RST_address);
            XmlDocument SoapXMLBody = new XmlDocument();
            string SSO_XML = $@"<?xml version=""1.0"" encoding=""utf-8"" ?>
            <Envelope xmlns=""http://schemas.xmlsoap.org/soap/envelope/""
               xmlns:wsse=""http://schemas.xmlsoap.org/ws/2003/06/secext""
               xmlns:saml=""urn:oasis:names:tc:SAML:1.0:assertion""
               xmlns:wsp=""http://schemas.xmlsoap.org/ws/2002/12/policy""
               xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd""
               xmlns:wsa=""http://schemas.xmlsoap.org/ws/2004/03/addressing""
               xmlns:wssc=""http://schemas.xmlsoap.org/ws/2004/04/sc""
               xmlns:wst=""http://schemas.xmlsoap.org/ws/2004/04/trust"">
               <Header>
                   <ps:AuthInfo
                       xmlns:ps=""http://schemas.microsoft.com/Passport/SoapServices/PPCRL""
                       Id=""PPAuthInfo"">
                       <ps:HostingApp>{{7108E71A-9926-4FCB-BCC9-9A9D3F32E423}}</ps:HostingApp>
                       <ps:BinaryVersion>4</ps:BinaryVersion>
                       <ps:UIVersion>1</ps:UIVersion>
                       <ps:Cookies></ps:Cookies>
                       <ps:RequestParams>AQAAAAIAAABsYwQAAAAxMDMz</ps:RequestParams>
                   </ps:AuthInfo>
                   <wsse:Security>
                       <wsse:UsernameToken Id=""user"">
                           <wsse:Username>{email}</wsse:Username>
                           <wsse:Password>{password}</wsse:Password>
                       </wsse:UsernameToken>
                   </wsse:Security>
               </Header>
               <Body>
                   <ps:RequestMultipleSecurityTokens
                       xmlns:ps=""http://schemas.microsoft.com/Passport/SoapServices/PPCRL""
                       Id=""RSTS"">
                       <wst:RequestSecurityToken Id=""RST0"">
                           <wst:RequestType>http://schemas.xmlsoap.org/ws/2004/04/security/trust/Issue</wst:RequestType>
                           <wsp:AppliesTo>
                               <wsa:EndpointReference>
                                   <wsa:Address>http://Passport.NET/tb</wsa:Address>
                               </wsa:EndpointReference>
                           </wsp:AppliesTo>
                       </wst:RequestSecurityToken>
                       <wst:RequestSecurityToken Id=""RST1"">
                           <wst:RequestType>http://schemas.xmlsoap.org/ws/2004/04/security/trust/Issue</wst:RequestType>
                           <wsp:AppliesTo>
                               <wsa:EndpointReference>
                                   <wsa:Address>messengerclear.live.com</wsa:Address>
                               </wsa:EndpointReference>
                           </wsp:AppliesTo>
                           <wsse:PolicyReference URI=""{MBIKeyOld}""></wsse:PolicyReference>
                       </wst:RequestSecurityToken>
                   </ps:RequestMultipleSecurityTokens>
               </Body>
            </Envelope>";
            SoapXMLBody.LoadXml(SSO_XML);
            using(Stream stream = SOAPRequest.GetRequestStream())
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

        public byte[] GetResultFromSSOHashs(string key, string ws_secure)
        {
            HMACSHA1 hMACSHA1 = new HMACSHA1(Encoding.UTF8.GetBytes(key));
            byte[] ws_secure_bytes = Encoding.UTF8.GetBytes(ws_secure);
            byte[] hash1 = hMACSHA1.ComputeHash(ws_secure_bytes);
            byte[] hash2 = hMACSHA1.ComputeHash(hash1.Concat(ws_secure_bytes).ToArray());
            byte[] hash3 = hMACSHA1.ComputeHash(hash1);
            byte[] hash4 = hMACSHA1.ComputeHash(hash3.Concat(ws_secure_bytes).ToArray());
            byte[] return_key = hash2;
            for (int i = 0; i < 4; i++)
            {
                return_key.Append(hash4[i]);
            }
            return return_key;
        }

        public string ReturnBinarySecret()
        {
            XmlDocument result_xml = new XmlDocument();
            result_xml.LoadXml(SOAPResult);
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(result_xml.NameTable);
            xmlNamespaceManager.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
            xmlNamespaceManager.AddNamespace("wsse", "http://schemas.xmlsoap.org/ws/2003/06/secext");
            xmlNamespaceManager.AddNamespace("wst", "http://schemas.xmlsoap.org/ws/2004/04/trust");
            string xPathString = "//S:Envelope/S:Body/wst:RequestSecurityTokenResponseCollection/wst:RequestSecurityTokenResponse/wst:RequestedTokenReference/wsse:Reference[@URI='#Compact1']";
            XmlNode RequestedTokenReferenceReference = result_xml.SelectSingleNode(xPathString, xmlNamespaceManager);
            XmlNode RequestSecurityTokenResponse1 = RequestedTokenReferenceReference.ParentNode.ParentNode;
            xPathString = "./wst:RequestedProofToken/wst:BinarySecret";
            XmlNode BinarySecretNode = RequestSecurityTokenResponse1.SelectSingleNode(xPathString, xmlNamespaceManager);
            return BinarySecretNode.InnerText;
        }

        public string ReturnTicket()
        {
            XmlDocument result_xml = new XmlDocument();
            result_xml.LoadXml(SOAPResult);
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(result_xml.NameTable);
            xmlNamespaceManager.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
            xmlNamespaceManager.AddNamespace("wsse", "http://schemas.xmlsoap.org/ws/2003/06/secext");
            xmlNamespaceManager.AddNamespace("wst", "http://schemas.xmlsoap.org/ws/2004/04/trust");
            string xPathString = "//S:Envelope/S:Body/wst:RequestSecurityTokenResponseCollection/wst:RequestSecurityTokenResponse/wst:RequestedSecurityToken/wsse:BinarySecurityToken[@Id='Compact1']";
            XmlNode BinarySecurityToken = result_xml.SelectSingleNode(xPathString, xmlNamespaceManager);
            return BinarySecurityToken.InnerText;
        }

        public void GetSSOReturnValue()
        {
            string binary_secret = ReturnBinarySecret();
            string ticket = ReturnTicket();
        }
    }
}
