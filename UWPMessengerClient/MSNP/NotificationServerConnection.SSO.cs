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
        private string mbiKeyOldNonce;
        private string ticketToken;

        private async Task MSNP15LoginToMessengerAsync()
        {
            nsSocket = new SocketCommands(nsAddress, port);
            Action loginAction = new Action(() =>
            {
                nsSocket.ConnectSocket();
                nsSocket.SetReceiveTimeout(25000);
                transactionId++;
                nsSocket.SendCommand($"VER {transactionId} MSNP15 CVR0\r\n");
                outputString = nsSocket.ReceiveMessage(receivedBytes);//receive VER response
                transactionId++;
                nsSocket.SendCommand($"CVR {transactionId} 0x0409 winnt 10 i386 UWPMESSENGER 0.6 msmsgs\r\n");
                outputString = nsSocket.ReceiveMessage(receivedBytes);//receive CVR response
                transactionId++;
                nsSocket.SendCommand($"USR {transactionId} SSO I {email}\r\n");
                outputString = nsSocket.ReceiveMessage(receivedBytes);//receive GCF
                outputString = nsSocket.ReceiveMessage(receivedBytes);//receive USR response with nonce
                transactionId++;
                UserInfo.Email = email;
                GetMbiKeyOldNonce();
                soapResult = soapRequests.SsoRequest(email, password, mbiKeyOldNonce);
                GetContactsFromDatabase();
                string response_struct = GetSSOReturnValue();
                nsSocket.SendCommand($"USR {transactionId} SSO S {ssoTicket} {response_struct}\r\n");//sending response to USR
                outputString = nsSocket.ReceiveMessage(receivedBytes);//receive USR OK
                nsSocket.BeginReceiving(receivedBytes, new AsyncCallback(ReceivingCallback), this);
                membershipLists = soapRequests.FindMembership();
                addressBook = soapRequests.AbFindAll();
                FillContactListFromSOAP();
                FillContactsInForwardListFromSOAP();
                SendBLP();
                SendInitialADL();
                SendUserDisplayName();
                transactionId++;
                nsSocket.SendCommand($"CHG {transactionId} {UserPresenceStatus} {clientCapabilities}\r\n");//setting presence as available
            });
            await Task.Run(loginAction);
        }

        public static byte[] JoinBytes(byte[] first, byte[] second)
        {
            return first.Concat(second).ToArray();
        }

        private byte[] GetResultFromSSOHashs(byte[] key, string wsSecure)
        {
            HMACSHA1 hMACSHA1 = new HMACSHA1(key);
            byte[] wsSecureBytes = Encoding.ASCII.GetBytes(wsSecure);
            byte[] hash1 = hMACSHA1.ComputeHash(wsSecureBytes);
            byte[] hash2 = hMACSHA1.ComputeHash(JoinBytes(hash1, wsSecureBytes));
            byte[] hash3 = hMACSHA1.ComputeHash(hash1);
            byte[] hash4 = hMACSHA1.ComputeHash(JoinBytes(hash3, wsSecureBytes));
            byte[] hash4Fourbytes = new byte[4];
            Buffer.BlockCopy(hash4, 0, hash4Fourbytes, 0, hash4Fourbytes.Length);
            byte[] returnKey = JoinBytes(hash2, hash4Fourbytes);
            return returnKey;
        }

        private string ReturnBinarySecret()
        {
            XmlDocument resultXml = new XmlDocument();
            resultXml.LoadXml(soapResult);
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(resultXml.NameTable);
            xmlNamespaceManager.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
            xmlNamespaceManager.AddNamespace("wsse", "http://schemas.xmlsoap.org/ws/2003/06/secext");
            xmlNamespaceManager.AddNamespace("wst", "http://schemas.xmlsoap.org/ws/2004/04/trust");
            string xPathString = "//S:Envelope/S:Body/wst:RequestSecurityTokenResponseCollection/wst:RequestSecurityTokenResponse/wst:RequestedTokenReference/wsse:Reference[@URI='#Compact2']";
            XmlNode requestedTokenReferenceReference = resultXml.SelectSingleNode(xPathString, xmlNamespaceManager);
            XmlNode requestSecurityTokenResponse1 = requestedTokenReferenceReference.ParentNode.ParentNode;
            xPathString = "./wst:RequestedProofToken/wst:BinarySecret";
            XmlNode binarySecretNode = requestSecurityTokenResponse1.SelectSingleNode(xPathString, xmlNamespaceManager);
            return binarySecretNode.InnerText;
        }

        public string ReturnTicket()
        {
            XmlDocument resultXml = new XmlDocument();
            resultXml.LoadXml(soapResult);
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(resultXml.NameTable);
            xmlNamespaceManager.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
            xmlNamespaceManager.AddNamespace("wsse", "http://schemas.xmlsoap.org/ws/2003/06/secext");
            xmlNamespaceManager.AddNamespace("wst", "http://schemas.xmlsoap.org/ws/2004/04/trust");
            string xPathString = "//S:Envelope/S:Body/wst:RequestSecurityTokenResponseCollection/wst:RequestSecurityTokenResponse/wst:RequestedSecurityToken/wsse:BinarySecurityToken[@Id='Compact2']";
            XmlNode binarySecurityToken = resultXml.SelectSingleNode(xPathString, xmlNamespaceManager);
            return binarySecurityToken.InnerText;
        }

        public byte[] ReturnByteArrayFromUIntArray(uint[] uintArray)
        {
            byte[] byteArray = new byte[sizeof(uint) * uintArray.Length];
            byte[] numberBytes;
            for (int i = 0; i < uintArray.Length; i++)
            {
                numberBytes = BitConverter.GetBytes(uintArray[i]);
                Buffer.BlockCopy(numberBytes, 0, byteArray, i * sizeof(uint), sizeof(uint));
            }
            return byteArray;
        }

        private void GetTicketToken()
        {
            XmlDocument resultXml = new XmlDocument();
            resultXml.LoadXml(soapResult);
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(resultXml.NameTable);
            xmlNamespaceManager.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
            xmlNamespaceManager.AddNamespace("wsse", "http://schemas.xmlsoap.org/ws/2003/06/secext");
            xmlNamespaceManager.AddNamespace("wst", "http://schemas.xmlsoap.org/ws/2004/04/trust");
            string xPathString = "//S:Envelope/S:Body/wst:RequestSecurityTokenResponseCollection/wst:RequestSecurityTokenResponse/wst:RequestedSecurityToken/wsse:BinarySecurityToken[@Id='Compact3']";
            XmlNode binerySecurityToken = resultXml.SelectSingleNode(xPathString, xmlNamespaceManager);
            ticketToken = binerySecurityToken.InnerText;
            ticketToken = ticketToken.Replace("&", "&amp;");
            soapRequests.TicketToken = ticketToken;
        }

        private string GetSSOReturnValue()
        {
            string binarySecret = ReturnBinarySecret();
            string ticket = ReturnTicket();
            GetTicketToken();
            ssoTicket = ticket;
            byte[] nonceBytes = Encoding.ASCII.GetBytes(mbiKeyOldNonce);
            byte[] key1 = Convert.FromBase64String(binarySecret);
            byte[] key2 = GetResultFromSSOHashs(key1, "WS-SecureConversationSESSION KEY HASH");
            byte[] key3 = GetResultFromSSOHashs(key1, "WS-SecureConversationSESSION KEY ENCRYPTION");
            HMACSHA1 hMACSHA1 = new HMACSHA1(key2);
            byte[] key2Hash = hMACSHA1.ComputeHash(nonceBytes);
            byte[] eight8Bytes = { 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08 };
            byte[] paddedNonce = JoinBytes(nonceBytes, eight8Bytes);
            byte[] randomBytes = new byte[8];
            RNGCryptoServiceProvider rNGCrypto = new RNGCryptoServiceProvider();
            rNGCrypto.GetBytes(randomBytes);
            byte[] encryptedData = new byte[72];
            TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider
            {
                Mode = CipherMode.CBC
            };
            tripleDES.CreateEncryptor(key3, randomBytes).TransformBlock(paddedNonce, 0, paddedNonce.Length, encryptedData, 0);
            uint[] headerValues =
            {
                28,//uStructHeaderSize
                1,//uCryptMode
                0x6603,//uCipherMode
                0x8004,//uHashType
                8,//uIVLen
                20,//uHashLen
                72//uCipherLen
            };
            byte[] returnStruct = ReturnByteArrayFromUIntArray(headerValues);
            returnStruct = JoinBytes(returnStruct, randomBytes);//aIVBytes
            returnStruct = JoinBytes(returnStruct, key2Hash);//aHashBytes
            returnStruct = JoinBytes(returnStruct, encryptedData);//aCipherBytes
            string returnValue = Convert.ToBase64String(returnStruct);
            return returnValue;
        }
    }
}
