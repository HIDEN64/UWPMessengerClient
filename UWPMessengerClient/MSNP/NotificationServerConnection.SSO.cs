using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml;
using System.Security.Cryptography;

namespace UWPMessengerClient.MSNP
{
    public partial class NotificationServerConnection
    {
        private string MBIKeyOldNonce;
        private string TicketToken;

        protected async Task MSNP15LoginToMessengerAsync()
        {
            NSSocket = new SocketCommands(NSaddress, port);
            Action loginAction = new Action(() =>
            {
                NSSocket.ConnectSocket();
                NSSocket.SetReceiveTimeout(15000);
                transactionID++;
                NSSocket.SendCommand($"VER {transactionID} MSNP15 CVR0\r\n");
                output_string = NSSocket.ReceiveMessage(received_bytes);//receive VER response
                transactionID++;
                NSSocket.SendCommand($"CVR {transactionID} 0x0409 winnt 10 i386 UWPMESSENGER 0.6 msmsgs\r\n");
                output_string = NSSocket.ReceiveMessage(received_bytes);//receive CVR response
                transactionID++;
                NSSocket.SendCommand($"USR {transactionID} SSO I {email}\r\n");
                output_string = NSSocket.ReceiveMessage(received_bytes);//receive GCF
                output_string = NSSocket.ReceiveMessage(received_bytes);//receive USR response with nonce
                transactionID++;
                userInfo.Email = email;
                GetMBIKeyOldNonce();
                SOAPResult = Perform_SSO_SOAP_Request();
                string response_struct = GetSSOReturnValue();
                NSSocket.BeginReceiving(received_bytes, new AsyncCallback(ReceivingCallback), this);
                NSSocket.SendCommand($"USR {transactionID} SSO S {SSO_Ticket} {response_struct}\r\n");//sending response to USR
                MembershipLists = MakeMembershipListsSOAPRequest();
                AddressBook = MakeAddressBookSOAPRequest();
                FillContactListFromSOAP();
                FillContactsInForwardListFromSOAP();
                SendBLP();
                SendInitialADL();
                SendUserDisplayName();
                transactionID++;
                NSSocket.SendCommand($"CHG {transactionID} {UserPresenceStatus} {clientCapabilities}\r\n");//setting presence as available
            });
            await Task.Run(loginAction);
        }

        protected string Perform_SSO_SOAP_Request()
        {
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
                            <wsse:PolicyReference URI=""{MBIKeyOldNonce}""></wsse:PolicyReference>
                        </wst:RequestSecurityToken>
                        <wst:RequestSecurityToken Id=""RST2"">
                            <wst:RequestType>http://schemas.xmlsoap.org/ws/2004/04/security/trust/Issue</wst:RequestType>
                            <wsp:AppliesTo>
                                <wsa:EndpointReference>
                                    <wsa:Address>contacts.msn.com</wsa:Address>
                                </wsa:EndpointReference>
                            </wsp:AppliesTo>
                            <wsse:PolicyReference URI=""?fs=1&amp;id=24000&amp;kv=9&amp;rn=93S9SWWw&amp;tw=0&amp;ver=2.1.6000.1"">
                            </wsse:PolicyReference>
                        </wst:RequestSecurityToken>
                    </ps:RequestMultipleSecurityTokens>
                </Body>
            </Envelope>";
            return MakeSOAPRequest(SSO_XML, RST_address, "http://www.msn.com/webservices/storage/w10/");
        }

        public static byte[] JoinBytes(byte[] first, byte[] second)
        {
            return first.Concat(second).ToArray();
        }

        protected byte[] GetResultFromSSOHashs(byte[] key, string ws_secure)
        {
            HMACSHA1 hMACSHA1 = new HMACSHA1(key);
            byte[] ws_secure_bytes = Encoding.ASCII.GetBytes(ws_secure);
            byte[] hash1 = hMACSHA1.ComputeHash(ws_secure_bytes);
            byte[] hash2 = hMACSHA1.ComputeHash(JoinBytes(hash1, ws_secure_bytes));
            byte[] hash3 = hMACSHA1.ComputeHash(hash1);
            byte[] hash4 = hMACSHA1.ComputeHash(JoinBytes(hash3, ws_secure_bytes));
            byte[] hash4_4bytes = new byte[4];
            Buffer.BlockCopy(hash4, 0, hash4_4bytes, 0, hash4_4bytes.Length);
            byte[] return_key = JoinBytes(hash2, hash4_4bytes);
            return return_key;
        }

        protected string ReturnBinarySecret()
        {
            XmlDocument result_xml = new XmlDocument();
            result_xml.LoadXml(SOAPResult);
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(result_xml.NameTable);
            xmlNamespaceManager.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
            xmlNamespaceManager.AddNamespace("wsse", "http://schemas.xmlsoap.org/ws/2003/06/secext");
            xmlNamespaceManager.AddNamespace("wst", "http://schemas.xmlsoap.org/ws/2004/04/trust");
            string xPathString = "//S:Envelope/S:Body/wst:RequestSecurityTokenResponseCollection/wst:RequestSecurityTokenResponse/wst:RequestedTokenReference/wsse:Reference[@URI='#Compact2']";
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
            string xPathString = "//S:Envelope/S:Body/wst:RequestSecurityTokenResponseCollection/wst:RequestSecurityTokenResponse/wst:RequestedSecurityToken/wsse:BinarySecurityToken[@Id='Compact2']";
            XmlNode BinarySecurityToken = result_xml.SelectSingleNode(xPathString, xmlNamespaceManager);
            return BinarySecurityToken.InnerText;
        }

        public byte[] ReturnByteArrayFromUIntArray(uint[] uint_array)
        {
            byte[] byte_array = new byte[sizeof(uint) * uint_array.Length];
            byte[] number_bytes;
            for (int i = 0; i < uint_array.Length; i++)
            {
                number_bytes = BitConverter.GetBytes(uint_array[i]);
                Buffer.BlockCopy(number_bytes, 0, byte_array, i * sizeof(uint), sizeof(uint));
            }
            return byte_array;
        }

        protected void GetTicketToken()
        {
            XmlDocument result_xml = new XmlDocument();
            result_xml.LoadXml(SOAPResult);
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(result_xml.NameTable);
            xmlNamespaceManager.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
            xmlNamespaceManager.AddNamespace("wsse", "http://schemas.xmlsoap.org/ws/2003/06/secext");
            xmlNamespaceManager.AddNamespace("wst", "http://schemas.xmlsoap.org/ws/2004/04/trust");
            string xPathString = "//S:Envelope/S:Body/wst:RequestSecurityTokenResponseCollection/wst:RequestSecurityTokenResponse/wst:RequestedSecurityToken/wsse:BinarySecurityToken[@Id='Compact3']";
            XmlNode BinarySecurityToken = result_xml.SelectSingleNode(xPathString, xmlNamespaceManager);
            TicketToken = BinarySecurityToken.InnerText;
            TicketToken = TicketToken.Replace("&", "&amp;");
        }

        protected string GetSSOReturnValue()
        {
            string binary_secret = ReturnBinarySecret();
            string ticket = ReturnTicket();
            GetTicketToken();
            SSO_Ticket = ticket;
            byte[] nonce_bytes = Encoding.ASCII.GetBytes(MBIKeyOldNonce);
            byte[] key1 = Convert.FromBase64String(binary_secret);
            byte[] key2 = GetResultFromSSOHashs(key1, "WS-SecureConversationSESSION KEY HASH");
            byte[] key3 = GetResultFromSSOHashs(key1, "WS-SecureConversationSESSION KEY ENCRYPTION");
            HMACSHA1 hMACSHA1 = new HMACSHA1(key2);
            byte[] key2_hash = hMACSHA1.ComputeHash(nonce_bytes);
            byte[] bytes_8 = { 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08 };
            byte[] padded_nonce = JoinBytes(nonce_bytes, bytes_8);
            byte[] random_bytes = new byte[8];
            RNGCryptoServiceProvider rNGCrypto = new RNGCryptoServiceProvider();
            rNGCrypto.GetBytes(random_bytes);
            byte[] encrypted_data = new byte[72];
            TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();
            tripleDES.Mode = CipherMode.CBC;
            tripleDES.CreateEncryptor(key3, random_bytes).TransformBlock(padded_nonce, 0, padded_nonce.Length, encrypted_data, 0);
            uint[] header_values =
            {
                28,//uStructHeaderSize
                1,//uCryptMode
                0x6603,//uCipherMode
                0x8004,//uHashType
                8,//uIVLen
                20,//uHashLen
                72//uCipherLen
            };
            byte[] return_struct = ReturnByteArrayFromUIntArray(header_values);
            return_struct = JoinBytes(return_struct, random_bytes);//aIVBytes
            return_struct = JoinBytes(return_struct, key2_hash);//aHashBytes
            return_struct = JoinBytes(return_struct, encrypted_data);//aCipherBytes
            string return_value = Convert.ToBase64String(return_struct);
            return return_value;
        }
    }
}
