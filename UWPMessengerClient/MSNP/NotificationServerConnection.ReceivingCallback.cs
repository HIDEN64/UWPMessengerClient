using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Xml;
using Windows.UI.Core;

namespace UWPMessengerClient.MSNP
{
    public partial class NotificationServerConnection
    {
        private string SOAPResult;
        private string SSO_Ticket;
        private byte[] received_bytes = new byte[4096];
        private string output_string;
        private string current_response;
        private string next_response;

        public void ReceivingCallback(IAsyncResult asyncResult)
        {
            NotificationServerConnection notificationServerConnection = (NotificationServerConnection)asyncResult.AsyncState;
            int bytes_read = notificationServerConnection.NSSocket.StopReceiving(asyncResult);
            notificationServerConnection.output_string = Encoding.UTF8.GetString(notificationServerConnection.received_bytes, 0, bytes_read);
            string[] responses = notificationServerConnection.output_string.Split("\r\n");
            for (var i = 0; i < responses.Length; i++)
            {
                string[] res_params = responses[i].Split(" ");
                notificationServerConnection.current_response = responses[i];
                if (i != responses.Length - 1)
                {
                    notificationServerConnection.next_response = responses[i + 1];
                }
                try
                {
                    notificationServerConnection.command_handlers[res_params[0]]();
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }
                catch (Exception e)
                {
                    var task = notificationServerConnection.AddToErrorLog($"{res_params[0]} processing error: " + e.Message);
                }
            }
            if (bytes_read > 0)
            {
                notificationServerConnection.NSSocket.BeginReceiving(notificationServerConnection.received_bytes, new AsyncCallback(ReceivingCallback), notificationServerConnection);
            }
        }

        protected void SeparateAndProcessCommandFromPayloadWithResponse(string response, int payload_size)
        {
            byte[] response_bytes = Encoding.UTF8.GetBytes(response);
            byte[] payload_bytes = new byte[payload_size];
            Buffer.BlockCopy(response_bytes, 0, payload_bytes, 0, payload_size);
            string payload = Encoding.UTF8.GetString(payload_bytes);
            string new_command = response.Replace(payload, "");
            current_response = new_command;
            string[] cmd_params = new_command.Split(" ");
            command_handlers[cmd_params[0]]();
        }

        protected string SeparatePayloadFromPayloadWithResponse(string response, int payload_size)
        {
            byte[] response_bytes = Encoding.UTF8.GetBytes(response);
            byte[] payload_bytes = new byte[payload_size];
            Buffer.BlockCopy(response_bytes, 0, payload_bytes, 0, payload_size);
            string payload = Encoding.UTF8.GetString(payload_bytes);
            return payload;
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

        public void HandleLST()
        {
            string[] LSTParams = current_response.Split(" ");
            string email, displayName, guid;
            int listbit = 0;
            email = LSTParams[1].Replace("N=","");
            displayName = LSTParams[2].Replace("F=", "");
            try
            {
                guid = LSTParams[3].Replace("C=", "");
            }
            catch (IndexOutOfRangeException)
            {
                guid = "";
            }
            int.TryParse(LSTParams[4], out listbit);
            displayName = PlusCharactersRegex.Replace(displayName, "");
            var contactInList = from contact_in_list in contact_list
                                where contact_in_list.email == email
                                select contact_in_list;
            if (!contactInList.Any())
            {
                Contact newContact = new Contact(listbit) { displayName = displayName, email = email, GUID = guid };
                contact_list.Add(newContact);
                DatabaseAccess.AddContactToTable(userInfo.Email, newContact);
                if (newContact.onForward)
                {
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        contacts_in_forward_list.Add(newContact);
                    });
                }
            }
            else
            {
                foreach (Contact contact_in_list in contactInList)
                {
                    contact_in_list.SetListsFromListbit(listbit);
                    contact_in_list.displayName = displayName;
                    contact_in_list.GUID = guid;
                    if (contact_in_list.onForward)
                    {
                        Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            contacts_in_forward_list.Add(contact_in_list);
                        });
                    }
                }
            }
        }

        public void HandleADC()
        {
            string[] ADCResponses = current_response.Split(" ");
            string email, displayName, guid;
            email = ADCResponses[3].Replace("N=", "");
            displayName = ADCResponses[4].Replace("F=", "");
            displayName = PlusCharactersRegex.Replace(displayName, "");
            guid = ADCResponses[5].Replace("C=", "");
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                contact_list.Add(new Contact((int)ListNumbers.Forward + (int)ListNumbers.Allow) { displayName = displayName, email = email, GUID = guid });
                contacts_in_forward_list.Add(contact_list.Last());
            });
        }

        public void HandlePRP()
        {
            string[] PRPParams = current_response.Split(" ");
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (PRPParams[1] == "MFN")
                {
                    string displayName = PlusCharactersRegex.Replace(PRPParams[2], "");
                    userInfo.displayName = displayName;
                }
                else if (PRPParams[2] == "MFN")
                {
                    string displayName = PlusCharactersRegex.Replace(PRPParams[3], "");
                    userInfo.displayName = displayName;
                }
            });
        }

        public void HandleILN()
        {
            //gets the parameters, does a LINQ query in the contact list and sets the contact's status
            string[] ILNParams = current_response.Split(" ");
            string status = ILNParams[2];
            string email = ILNParams[3];
            string displayName = "";
            switch (MSNPVersion)
            {
                case "MSNP12":
                    displayName = ILNParams[4];
                    break;
                case "MSNP15":
                    displayName = ILNParams[5];
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
            displayName = PlusCharactersRegex.Replace(displayName, "");
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var contactWithPresence = from contact in contact_list
                                          where contact.email == email
                                          select contact;
                foreach (Contact contact in contactWithPresence)
                {
                    contact.presenceStatus = status;
                    contact.displayName = displayName;
                    DatabaseAccess.UpdateContact(userInfo.Email, contact);
                }
            });
        }

        public void HandleNLN()
        {
            //gets the parameters, does a LINQ query in the contact list and sets the contact's status
            string[] NLNParams = current_response.Split(" ");
            string status = NLNParams[1];
            string email = NLNParams[2];
            string displayName = "";
            switch (MSNPVersion)
            {
                case "MSNP12":
                    displayName = NLNParams[3];
                    break;
                case "MSNP15":
                    displayName = NLNParams[4];
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
            displayName = PlusCharactersRegex.Replace(displayName, "");
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var contactWithPresence = from contact in contact_list
                                          where contact.email == email
                                          select contact;
                foreach (Contact contact in contactWithPresence)
                {
                    contact.presenceStatus = status;
                    contact.displayName = displayName;
                    DatabaseAccess.UpdateContact(userInfo.Email, contact);
                }
            });
        }

        public void HandleFLN()
        {
            string[] FLNParams = current_response.Split(" ");
            //for the FLN response gets the email, does a LINQ query in the contact list and sets the contact's status to offline
            string email = FLNParams[1];
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var contactWithPresence = from contact in contact_list
                                            where contact.email == email
                                            select contact;
                foreach (Contact contact in contactWithPresence)
                {
                    contact.presenceStatus = null;
                }
            });
        }

        public void HandleUBX()
        {
            string personal_message;
            string[] UBXParams = current_response.Split(" ");
            string principal_email = UBXParams[1];
            string length_str = "";
            switch (MSNPVersion)
            {
                case "MSNP12":
                    length_str = UBXParams[2];
                    break;
                case "MSNP15":
                    length_str = UBXParams[3];
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
            int ubx_length;
            int.TryParse(length_str, out ubx_length);
            string payload = SeparatePayloadFromPayloadWithResponse(next_response, ubx_length);
            XmlDocument personalMessagePayload = new XmlDocument();
            try
            {
                personalMessagePayload.LoadXml(payload);
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
                    DatabaseAccess.UpdateContact(userInfo.Email, contact);
                }
            });
            SeparateAndProcessCommandFromPayloadWithResponse(next_response, ubx_length);
        }

        public async Task HandleXFR()
        {
            string[] XFRParams = current_response.Split(" ");
            string[] address_and_port = XFRParams[3].Split(":");
            string sb_address = address_and_port[0];
            int sb_port;
            int.TryParse(address_and_port[1], out sb_port);
            string auth_string = "";
            switch (MSNPVersion)
            {
                case "MSNP12":
                    auth_string = XFRParams.Last();
                    break;
                case "MSNP15":
                    auth_string = XFRParams[5];
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
            SBConnection.SetAddressPortAndAuthString(sb_address, sb_port, auth_string);
            await SBConnection.LoginToNewSwitchboardAsync();
            await SBConnection.InvitePrincipal(contacts_in_forward_list[ContactIndexToChat].email, contacts_in_forward_list[ContactIndexToChat].displayName);
        }

        public void HandleRNG()
        {
            string[] RNGParams = current_response.Split(" ");
            string sessionID = RNGParams[1];
            string[] address_and_port = RNGParams[2].Split(":");
            int sb_port;
            string sb_address = address_and_port[0];
            int.TryParse(address_and_port[1], out sb_port);
            string authString = RNGParams[4];
            string principalEmail = RNGParams[5];
            string principalName = RNGParams[6];
            SwitchboardConnection switchboardConnection = new SwitchboardConnection(sb_address, sb_port, email, authString, userInfo.displayName, principalName, principalEmail, sessionID);
            SBConnection = switchboardConnection;
            _ = SBConnection.AnswerRNG();
        }
    }
}
