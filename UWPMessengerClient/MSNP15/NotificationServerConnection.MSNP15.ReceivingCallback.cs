using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Xml;
using Windows.UI.Core;

namespace UWPMessengerClient.MSNP15
{
    public partial class NotificationServerConnection
    {
        private string SOAPResult;
        private string SSO_Ticket;
        private byte[] received_bytes = new byte[4096];
        private string output_string;
        public ObservableCollection<Contact> contact_list { get; set; } = new ObservableCollection<Contact>();
        public ObservableCollection<Contact> contacts_in_forward_list { get; set; } = new ObservableCollection<Contact>();
        public UserInfo userInfo { get; set; } = new UserInfo();

        public void MSNP15ReceivingCallback(IAsyncResult asyncResult)
        {
            NotificationServerConnection notificationServerConnection = (NotificationServerConnection)asyncResult.AsyncState;
            int bytes_read = notificationServerConnection.NSSocket.StopReceiving(asyncResult);
            notificationServerConnection.output_string = Encoding.UTF8.GetString(notificationServerConnection.received_bytes, 0, bytes_read);
            if (notificationServerConnection.output_string.Contains("PRP "))
            {
                if (notificationServerConnection.output_string.Contains("MFN"))
                {
                    notificationServerConnection.GetUserDisplayName();
                }
            }
            if (notificationServerConnection.output_string.Contains("ILN "))
            {
                notificationServerConnection.SetInitialContactPresence();
            }
            if (notificationServerConnection.output_string.StartsWith("NLN "))
            {
                notificationServerConnection.SetContactPresence();
            }
            if (notificationServerConnection.output_string.Contains("UBX "))
            {
                notificationServerConnection.GetContactsPersonalMessages();
            }
            if (notificationServerConnection.output_string.StartsWith("FLN "))
            {
                notificationServerConnection.SetContactOffline();
            }
            if (notificationServerConnection.output_string.StartsWith("XFR "))
            {
                var task = notificationServerConnection.ConnectToSwitchboard();
            }
            if (notificationServerConnection.output_string.StartsWith("RNG "))
            {
                notificationServerConnection.JoinSwitchboard();
            }
            if (bytes_read > 0)
            {
                notificationServerConnection.NSSocket.BeginReceiving(notificationServerConnection.received_bytes, new AsyncCallback(MSNP15ReceivingCallback), notificationServerConnection);
            }
        }

        protected void GetMBIKeyOldNonce()
        {
            string[] USRResponse = output_string.Split("USR ", 2);
            //ensuring the last element of the USRReponse array is just the USR response
            int rnIndex = USRResponse.Last().IndexOf("\r\n");
            if (rnIndex != USRResponse.Last().Length && rnIndex >= 0)
            {
                USRResponse[USRResponse.Length - 1] = USRResponse.Last().Remove(rnIndex);
            }
            string[] USRParams = USRResponse[1].Split(" ");
            string mbi_key_old = USRParams[4];
            MBIKeyOldNonce = mbi_key_old;
        }

        public void GetUserDisplayName()
        {
            string[] PRPResponse = output_string.Split("PRP ", 2);
            //ensuring the last element of the PRPReponses array is just the PRP response
            int rnIndex = PRPResponse.Last().IndexOf("\r\n");
            if (rnIndex != PRPResponse.Last().Length && rnIndex >= 0)
            {
                PRPResponse[PRPResponse.Length - 1] = PRPResponse.Last().Remove(rnIndex);
            }
            string[] PRPParams = PRPResponse[1].Split(" ");
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (int.TryParse(PRPParams[0], out int commandNumber))
                {
                    userInfo.displayName = PRPParams[2];
                }
                else
                {
                    userInfo.displayName = PRPParams[1];
                }
            });
        }

        public void SetInitialContactPresence()
        {
            string[] ILNResponses = output_string.Split("ILN ");
            //ensuring the last element of the ILNReponses array is just the ILN response
            int rnIndex = ILNResponses.Last().IndexOf("\r\n");
            if (rnIndex != ILNResponses.Last().Length && rnIndex >= 0)
            {
                ILNResponses[ILNResponses.Length - 1] = ILNResponses.Last().Remove(rnIndex);
            }
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                for (int i = 1; i < ILNResponses.Length; i++)
                {
                    //for each ILN response gets the parameters, does a LINQ query in the contact list and sets the contact's status
                    string[] ILNParams = ILNResponses[i].Split(" ");
                    string status = ILNParams[1];
                    string email = ILNParams[2];
                    var contactWithPresence = from contact in contact_list
                                              where contact.email == email
                                              select contact;
                    foreach (Contact contact in contactWithPresence)
                    {
                        contact.presenceStatus = status;
                    }
                }
            });
        }

        public void SetContactPresence()
        {
            string[] NLNResponses = output_string.Split("NLN ", 2);
            //ensuring the last element of the NLNReponses array is just the NLN response
            int rnIndex = NLNResponses.Last().IndexOf("\r\n");
            rnIndex += 2;//count for the \r and \n characters
            if (rnIndex != NLNResponses.Last().Length && rnIndex >= 0)
            {
                NLNResponses[NLNResponses.Length - 1] = NLNResponses.Last().Remove(rnIndex);
            }
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                for (int i = 1; i < NLNResponses.Length; i++)
                {
                    //for each ILN response gets the parameters, does a LINQ query in the contact list and sets the contact's status
                    string[] NLNParams = NLNResponses[i].Split(" ");
                    string status = NLNParams[0];
                    string email = NLNParams[1];
                    string displayName = NLNParams[3];
                    var contactWithPresence = from contact in contact_list
                                              where contact.email == email
                                              select contact;
                    foreach (Contact contact in contactWithPresence)
                    {
                        contact.presenceStatus = status;
                        contact.displayName = displayName;
                    }
                }
            });
        }

        public void SetContactOffline()
        {
            string[] FLNResponses = output_string.Split("FLN ", 2);
            //ensuring the last element of the FLNReponses array is just the FLN response
            int rnIndex = FLNResponses.Last().IndexOf("\r\n");
            if (rnIndex != FLNResponses.Last().Length && rnIndex >= 0)
            {
                FLNResponses[FLNResponses.Length - 1] = FLNResponses.Last().Remove(rnIndex);
            }
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                for (int i = 1; i < FLNResponses.Length; i++)
                {
                    //for each FLN response gets the email, does a LINQ query in the contact list and sets the contact's status to offline
                    string email = FLNResponses[i].Split(" ")[0];
                    var contactWithPresence = from contact in contact_list
                                              where contact.email == email
                                              select contact;
                    foreach (Contact contact in contactWithPresence)
                    {
                        contact.presenceStatus = null;
                    }
                }
            });
        }

        public void GetContactsPersonalMessages()
        {
            string[] UBXResponses = output_string.Split("UBX ");
            //ensuring the last element of the UBXReponses array is just the UBX responses
            for (var i = 1; i < UBXResponses.Length; i++)
            {
                int DataEndIndex = UBXResponses[i].LastIndexOf(">");
                int IndexToStartRemoving = DataEndIndex + 1;//remove just after the last xml tag
                if (IndexToStartRemoving != UBXResponses[i].Length && IndexToStartRemoving >= 0)
                {
                    UBXResponses[i] = UBXResponses[i].Remove(IndexToStartRemoving);
                }
                string personal_message;
                string[] UBXParams = UBXResponses[i].Split(" ");
                string principal_email = UBXParams[0];
                string length_str = UBXParams[2].Replace("\r\n", "");
                length_str = length_str.Remove(length_str.IndexOf("<"));
                int ubx_length;
                int.TryParse(length_str, out ubx_length);
                int indexData1 = UBXResponses[i].IndexOf("<Data>");
                byte[] personal_message_xml_buffer = new byte[ubx_length];
                byte[] ubx_response_buffer = Encoding.UTF8.GetBytes(UBXResponses[i]);
                Buffer.BlockCopy(ubx_response_buffer, indexData1, personal_message_xml_buffer, 0, ubx_length);
                string personal_message_xml = Encoding.UTF8.GetString(personal_message_xml_buffer);
                XmlDocument personalMessagePayload = new XmlDocument();
                try
                {
                    personalMessagePayload.LoadXml(personal_message_xml);
                    string xPath = "//Data/PSM";
                    XmlNode PSM = personalMessagePayload.SelectSingleNode(xPath);
                    personal_message = PSM.InnerText;
                }
                catch (XmlException)
                {
                    personal_message = "XML error";
                }
                Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var contactWithPersonalMessage = from contact in contact_list
                                                     where contact.email == principal_email
                                                     select contact;
                    foreach (Contact contact in contactWithPersonalMessage)
                    {
                        contact.personalMessage = personal_message;
                    }
                });
            }
        }

        public async Task ConnectToSwitchboard()
        {
            string[] XFRResponse = output_string.Split("XFR ", 2);
            //ensuring the last element of the XFRReponse array is just the XFR response
            int rnIndex = XFRResponse.Last().IndexOf("\r\n");
            if (rnIndex != XFRResponse.Last().Length && rnIndex >= 0)
            {
                XFRResponse[XFRResponse.Length - 1] = XFRResponse.Last().Remove(rnIndex);
            }
            string[] XFRParams = XFRResponse[1].Split(" ");
            string[] address_and_port = XFRParams[2].Split(":");
            string sb_address = address_and_port[0];
            int sb_port;
            int.TryParse(address_and_port[1], out sb_port);
            string trID = XFRParams[4];
            SBConnection.SetAddressPortAndTrID(sb_address, sb_port, trID);
            await SBConnection.LoginToNewSwitchboardAsync();
            await SBConnection.InvitePrincipal(contacts_in_forward_list[ContactIndexToChat].email, contacts_in_forward_list[ContactIndexToChat].displayName);
        }

        public void JoinSwitchboard()
        {
            string[] RNGResponse = output_string.Split("RNG ", 2);
            //ensuring the last element of the RNGReponse array is just the RNG response
            int rnIndex = RNGResponse.Last().IndexOf("\r\n");
            if (rnIndex != RNGResponse.Last().Length && rnIndex >= 0)
            {
                RNGResponse[RNGResponse.Length - 1] = RNGResponse.Last().Remove(rnIndex);
            }
            string[] RNGParams = RNGResponse[1].Split(" ");
            string sessionID = RNGParams[0];
            string[] address_and_port = RNGParams[1].Split(":");
            int sb_port;
            string sb_address = address_and_port[0];
            int.TryParse(address_and_port[1], out sb_port);
            string trID = RNGParams[3];
            string principalName = RNGParams[5];
            SwitchboardConnection switchboardConnection = new SwitchboardConnection(sb_address, sb_port, email, trID, userInfo.displayName, principalName, sessionID);
            SBConnection = switchboardConnection;
            _ = SBConnection.AnswerRNG();
        }
    }
}
