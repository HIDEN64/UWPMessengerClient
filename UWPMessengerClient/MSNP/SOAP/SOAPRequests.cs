using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace UWPMessengerClient.MSNP.SOAP
{
    partial class SOAPRequests
    {
        private string rstAddress = "https://m1.escargot.chat/RST.srf";
        public string TicketToken { get; set; }
        public bool UsingLocalhost { get; private set; }

        public SOAPRequests(bool useLocalhost = false)
        {
            UsingLocalhost = useLocalhost;
            if (UsingLocalhost)
            {
                rstAddress = "http://localhost/RST.srf";
                sharingServiceUrl = "http://localhost/abservice/SharingService.asmx";
                abServiceUrl = "http://localhost/abservice/abservice.asmx";
                //setting local addresses
            }
        }

        public static HttpWebRequest CreateSoapRequest(string soapAction, string address)
        {
            HttpWebRequest request = WebRequest.CreateHttp(address);
            request.Headers.Add($@"SOAPAction:{soapAction}");
            request.ContentType = "text/xml;charset=\"utf-8\"";
            request.Accept = "text/xml";
            request.Method = "POST";
            return request;
        }

        public static string MakeSoapRequest(string soapBody, string address, string soapAction)
        {
            HttpWebRequest soapRequest = CreateSoapRequest(soapAction, address);
            XmlDocument soapXmlBody = new XmlDocument();
            soapXmlBody.LoadXml(soapBody);
            using (Stream stream = soapRequest.GetRequestStream())
            {
                soapXmlBody.Save(stream);
            }
            using (WebResponse webResponse = soapRequest.GetResponse())
            {
                using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                {
                    var result = rd.ReadToEnd();
                    return result;
                }
            }
        }

        public string SsoRequest(string email, string password, string mbiKeyOldNonce)
        {
            string ssoXml = $@"<?xml version=""1.0"" encoding=""utf-8"" ?>
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
                        <ps:RequestParameters>AQAAAAIAAABsYwQAAAAxMDMz</ps:RequestParameters>
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
                            <wsse:PolicyReference URI=""{mbiKeyOldNonce}""></wsse:PolicyReference>
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
            return MakeSoapRequest(ssoXml, rstAddress, "http://www.msn.com/webservices/storage/w10/");
        }
    }
}
