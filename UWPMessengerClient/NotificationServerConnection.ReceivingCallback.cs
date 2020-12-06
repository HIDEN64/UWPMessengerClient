using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace UWPMessengerClient
{
    public partial class NotificationServerConnection
    {
        private byte[] received_bytes = new byte[4096];
        private string output_string;
        public ObservableCollection<Contact> contact_list { get; set; } = new ObservableCollection<Contact>();
        public ObservableCollection<Contact> contacts_in_forward_list { get; set; } = new ObservableCollection<Contact>();
        public UserInfo userInfo { get; set; } = new UserInfo();
        public SwitchboardConnection SBConnection { get; set; }

        public static void ReceivingCallback(IAsyncResult asyncResult)
        {
            NotificationServerConnection NServerConnection = (NotificationServerConnection)asyncResult.AsyncState;
            int bytes_read = NServerConnection.NSSocket.StopReceiving(asyncResult);
            NServerConnection.output_string = Encoding.UTF8.GetString(NServerConnection.received_bytes, 0, bytes_read);
            if (NServerConnection.output_string.Contains("LST "))
            {
                NServerConnection.CreateContactList();
            }
            if (NServerConnection.output_string.Contains("ILN "))
            {
                NServerConnection.SetInitialContactPresence();
            }
            if (NServerConnection.output_string.Contains("PRP "))
            {
                if (NServerConnection.output_string.Contains("MFN" ))
                {
                    NServerConnection.GetUserDisplayName();
                }
            }
            if (NServerConnection.output_string.StartsWith("NLN "))
            {
                NServerConnection.SetContactPresence();
            }
            if (NServerConnection.output_string.Contains("UBX "))
            {
                NServerConnection.GetContactPersonalMessage();
            }
            if (NServerConnection.output_string.StartsWith("FLN "))
            {
                NServerConnection.SetContactOffline();
            }
            if (NServerConnection.output_string.StartsWith("XFR "))
            {
                var task = NServerConnection.ConnectToSwitchboard();
            }
            if (NServerConnection.output_string.StartsWith("RNG "))
            {
                NServerConnection.JoinSwitchboard();
            }
            if (NServerConnection.output_string.StartsWith("ADC "))
            {
                NServerConnection.ReceiveNewContact();
            }
            if (bytes_read > 0)
            {
                NServerConnection.NSSocket.BeginReceiving(NServerConnection.received_bytes, new AsyncCallback(ReceivingCallback), NServerConnection);
            }
        }

        public void CreateContactList()
        {
            string[] LSTResponses = output_string.Split("LST ");
            //ensuring the last element of the LSTResponses array is just the LST response
            int rnIndex = LSTResponses.Last().IndexOf("\r\n");
            if (rnIndex != LSTResponses.Last().Length && rnIndex >= 0)
            {
                LSTResponses[LSTResponses.Length - 1] = LSTResponses[LSTResponses.Length - 1].Remove(rnIndex);
            }
            string email, displayName, guid;
            int listbit = 0;
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                for (int i = 1; i < LSTResponses.Length; i++)
                {
                    email = LSTResponses[i].Split("N=")[1];
                    email = email.Remove(email.IndexOf(" "));
                    displayName = LSTResponses[i].Split("F=")[1];
                    displayName = displayName.Remove(displayName.IndexOf(" "));
                    guid = LSTResponses[i].Split("C=")[1];
                    if (guid.Length > 1 && guid.IndexOf(" ") > 0)
                    {
                        guid = guid.Remove(guid.IndexOf(" "));
                    }
                    string[] LSTAndParams = LSTResponses[i].Split(" ");
                    if (int.TryParse(LSTAndParams[LSTAndParams.Length - 2], out listbit))
                    {
                        int.TryParse(LSTAndParams[LSTAndParams.Length - 3], out listbit);
                    }
                    else
                    {
                        int.TryParse(LSTAndParams[LSTAndParams.Length - 4], out listbit);
                    }
                    contact_list.Add(new Contact(listbit) { displayName = displayName, email = email, GUID = guid });
                }
                FillForwardListCollection();
            });
        }

        public void ReceiveNewContact()
        {
            string[] ADCResponses = output_string.Split("ADC ");
            //ensuring the last element of the ADCResponses array is just the ADC response
            int rnIndex = ADCResponses.Last().IndexOf("\r\n");
            if (rnIndex != ADCResponses.Last().Length && rnIndex >= 0)
            {
                ADCResponses[ADCResponses.Length - 1] = ADCResponses[ADCResponses.Length - 1].Remove(rnIndex);
            }
            string email, displayName, guid;
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                for (int i = 1; i < ADCResponses.Length; i++)
                {
                    email = ADCResponses[i].Split("N=")[1];
                    email = email.Remove(email.IndexOf(" "));
                    displayName = ADCResponses[i].Split("F=")[1];
                    displayName = displayName.Remove(displayName.IndexOf(" "));
                    guid = ADCResponses[i].Split("C=")[1];
                    if (guid.Length > 1 && guid.IndexOf(" ") > 0)
                    {
                        guid = guid.Remove(guid.IndexOf(" "));
                    }
                    contact_list.Add(new Contact(1) { displayName = displayName, email = email, GUID = guid });//1 for forward list
                    contacts_in_forward_list.Add(contact_list.Last());
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
                    string displayName = NLNParams[2];
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
                    string email = FLNResponses[i];
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

        public void GetContactPersonalMessage()
        {
            string[] UBXResponse = output_string.Split("UBX ", 2);
            //ensuring the last element of the UBXReponse array is just the UBX response
            int DataEndIndex = UBXResponse.Last().LastIndexOf(">");
            int IndexToStartRemoving = DataEndIndex + 1;//remove just after the last xml tag
            if (IndexToStartRemoving != UBXResponse.Last().Length && IndexToStartRemoving >= 0)
            {
                UBXResponse[UBXResponse.Length - 1] = UBXResponse.Last().Remove(IndexToStartRemoving);
            }
            string personal_message;
            string[] UBXParams = UBXResponse[1].Split(" ");
            string principal_email = UBXParams[0];
            string length_str = UBXParams[1].Replace("\r\n", "");
            length_str = length_str.Remove(length_str.IndexOf("<"));
            int ubx_length;
            int.TryParse(length_str, out ubx_length);
            int indexData1 = UBXResponse.Last().IndexOf("<Data>");
            byte[] personal_message_xml_buffer = new byte[ubx_length];
            byte[] ubx_response_buffer = Encoding.UTF8.GetBytes(UBXResponse.Last());
            Buffer.BlockCopy(ubx_response_buffer, indexData1, personal_message_xml_buffer, 0, ubx_length);
            string personal_message_xml = Encoding.UTF8.GetString(personal_message_xml_buffer);
            XElement personalMessageElement;
            try
            {
                personalMessageElement = XElement.Parse(personal_message_xml);
                personal_message = personalMessageElement.Value;
            }
            catch (System.Xml.XmlException)
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
            string trID = XFRParams.Last();
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
