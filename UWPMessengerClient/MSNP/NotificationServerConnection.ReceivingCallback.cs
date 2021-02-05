using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Xml;
using Windows.UI.Core;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Microsoft.QueryStringDotNET;
using Newtonsoft.Json;

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
        public event EventHandler<SwitchboardEventArgs> SwitchboardCreated;

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

        protected void SeparateAndProcessCommandFromResponse(string response, int payload_size)
        {
            if (response.Contains("\r\n"))
            {
                response = response.Split("\r\n", 2)[1];
            }
            byte[] response_bytes = Encoding.UTF8.GetBytes(response);
            byte[] payload_bytes = new byte[payload_size];
            Buffer.BlockCopy(response_bytes, 0, payload_bytes, 0, payload_size);
            string payload = Encoding.UTF8.GetString(payload_bytes);
            string new_command = response.Replace(payload, "");
            if (new_command != "")
            {
                current_response = new_command;
                string[] cmd_params = new_command.Split(" ");
                command_handlers[cmd_params[0]]();
            }
        }

        protected string SeparatePayloadFromResponse(string response, int payload_size)
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
            int listnumber = 0;
            email = LSTParams[1].Replace("N=","");
            displayName = LSTParams[2].Replace("F=", "");
            try
            {
                guid = LSTParams[3].Replace("C=", "");
            }
            catch (IndexOutOfRangeException)
            {
                guid = null;
            }
            int.TryParse(LSTParams[4], out listnumber);
            displayName = PlusCharactersRegex.Replace(displayName, "");
            var contactInList = from contact_in_list in ContactList
                                where contact_in_list.Email == email
                                select contact_in_list;
            if (!contactInList.Any())
            {
                Contact newContact = new Contact(listnumber)
                {
                    displayName = displayName,
                    Email = email,
                    GUID = guid
                };
                ContactList.Add(newContact);
                DatabaseAccess.AddContactToTable(userInfo.Email, newContact);
                if (newContact.onForward)
                {
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        ContactsInForwardList.Add(newContact);
                    });
                }
                if (newContact.onReverse || newContact.Pending)
                {
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        ContactsInPendingOrReverseList.Add(newContact);
                    });
                }
            }
            else
            {
                foreach (Contact contact_in_list in contactInList)
                {
                    contact_in_list.SetListsFromListnumber(listnumber);
                    contact_in_list.displayName = displayName;
                    contact_in_list.GUID = guid;
                    if (contact_in_list.onForward)
                    {
                        Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            ContactsInForwardList.Add(contact_in_list);
                        });
                    }
                    if (contact_in_list.onReverse || contact_in_list.Pending)
                    {
                        Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            ContactsInPendingOrReverseList.Add(contact_in_list);
                        });
                    }
                }
            }
        }

        public void HandleADC()
        {
            string[] ADCParams = current_response.Split(" ");
            string email, displayName;
            email = ADCParams[3].Replace("N=", "");
            displayName = ADCParams[4].Replace("F=", "");
            displayName = PlusCharactersRegex.Replace(displayName, "");
            if (ADCParams[2] == "RL")
            {
                var contactInList = from contact_in_list in ContactList
                                    where contact_in_list.Email == email
                                    select contact_in_list;
                if (!contactInList.Any())
                {
                    Contact newContact = new Contact((int)ListNumbers.Reverse)
                    {
                        Email = email,
                        displayName = displayName
                    };
                    ContactList.Add(newContact);
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        ContactsInPendingOrReverseList.Add(newContact);
                    });
                    ShowNewContactToast(newContact);
                }
                else
                {
                    foreach (Contact contact in contactInList)
                    {
                        contact.onReverse = true;
                        Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            ContactsInPendingOrReverseList.Add(contact);
                        });
                        ShowNewContactToast(contact);
                    }
                }
            }
        }

        public void HandleADL()
        {
            string[] ADLParams = current_response.Split(" ");
            if (ADLParams[2] == "OK") { return; }
            int payload_length;
            int.TryParse(ADLParams[2], out payload_length);
            string payload = SeparatePayloadFromResponse(next_response, payload_length);
            XmlDocument payload_xml = new XmlDocument();
            payload_xml.LoadXml(payload);
            XmlNode d_node = payload_xml.SelectSingleNode("//ml/d");
            XmlNode c_node = payload_xml.SelectSingleNode("//ml/d/c");
            XmlAttribute domain = d_node.Attributes["n"];
            XmlAttribute email_name = c_node.Attributes["n"];
            XmlAttribute display_name = c_node.Attributes["f"];
            XmlAttribute listnumber_attr = c_node.Attributes["l"];
            string email = email_name.InnerText + "@" + domain.InnerText;
            string displayName = display_name.InnerText;
            int listnumber;
            int.TryParse(listnumber_attr.InnerText, out listnumber);
            var contactInList = from contact_in_list in ContactList
                                where contact_in_list.Email == email
                                select contact_in_list;
            if (!contactInList.Any())
            {
                Contact newContact = new Contact(listnumber)
                {
                    Email = email,
                    displayName = displayName
                };
                ContactList.Add(newContact);
                if ((newContact.onReverse || newContact.Pending) && !newContact.onForward)
                {
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        ContactsInPendingOrReverseList.Add(newContact);
                    });
                    ShowNewContactToast(newContact);
                }
            }
            else
            {
                foreach (Contact contact in contactInList)
                {
                    contact.UpdateListsFromListnumber(listnumber);
                    if ((contact.onReverse || contact.Pending) && !contact.onForward)
                    {
                        Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            ContactsInPendingOrReverseList.Add(contact);
                        });
                        ShowNewContactToast(contact);
                    }
                }
            }
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
                var contactWithPresence = from contact in ContactList
                                          where contact.Email == email
                                          select contact;
                foreach (Contact contact in contactWithPresence)
                {
                    contact.presenceStatus = status;
                    contact.displayName = displayName;
                    DatabaseAccess.UpdateContact(userInfo.Email, contact);
                }
                var contactWithPresenceInForward = from contact in ContactsInForwardList
                                          where contact.Email == email
                                          select contact;
                foreach (Contact contact in contactWithPresenceInForward)
                {
                    contact.presenceStatus = status;
                    contact.displayName = displayName;
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
                var contactWithPresence = from contact in ContactList
                                          where contact.Email == email
                                          select contact;
                foreach (Contact contact in contactWithPresence)
                {
                    contact.presenceStatus = status;
                    contact.displayName = displayName;
                    DatabaseAccess.UpdateContact(userInfo.Email, contact);
                }
                var contactWithPresenceInForward = from contact in ContactsInForwardList
                                                   where contact.Email == email
                                                   select contact;
                foreach (Contact contact in contactWithPresenceInForward)
                {
                    contact.presenceStatus = status;
                    contact.displayName = displayName;
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
                var contactWithPresence = from contact in ContactList
                                            where contact.Email == email
                                            select contact;
                foreach (Contact contact in contactWithPresence)
                {
                    contact.presenceStatus = null;
                }
                var contactWithPresenceInForward = from contact in ContactsInForwardList
                                                   where contact.Email == email
                                                   select contact;
                foreach (Contact contact in contactWithPresenceInForward)
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
            string payload = SeparatePayloadFromResponse(next_response, ubx_length);
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
                var contactWithPersonalMessage = from contact in ContactList
                                                 where contact.Email == principal_email
                                                 select contact;
                foreach (Contact contact in contactWithPersonalMessage)
                {
                    contact.personalMessage = personal_message;
                    DatabaseAccess.UpdateContact(userInfo.Email, contact);
                }
                var contactWithPersonalMessageInForward = from contact in ContactsInForwardList
                                                 where contact.Email == principal_email
                                                 select contact;
                foreach (Contact contact in contactWithPersonalMessageInForward)
                {
                    contact.personalMessage = personal_message;
                }
            });
            SeparateAndProcessCommandFromResponse(next_response, ubx_length);
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
            SwitchboardConnection switchboardConnection = new SwitchboardConnection(sb_address, sb_port, email, auth_string, userInfo.displayName)
            {
                KeepMessagingHistory = KeepMessagingHistoryInSwitchboard
            };
            await switchboardConnection.LoginToNewSwitchboardAsync();
            await switchboardConnection.InvitePrincipal(ContactToChat.Email, ContactToChat.displayName);
            SwitchboardCreated?.Invoke(this, new SwitchboardEventArgs() { switchboard = switchboardConnection});
            switchboardConnection.FillMessageHistory();
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
            int conv_id = random.Next(1000, 9999);
            SBConversation conversation = new SBConversation(this, Convert.ToString(conv_id));
            SBConversations.Add(conversation);
            SwitchboardConnection switchboardConnection = new SwitchboardConnection(sb_address, sb_port, email, authString, userInfo.displayName, principalName, principalEmail, sessionID)
            {
                KeepMessagingHistory = KeepMessagingHistoryInSwitchboard
            };
            _ = switchboardConnection.AnswerRNG();
            SwitchboardCreated?.Invoke(this, new SwitchboardEventArgs() { switchboard = switchboardConnection });
            switchboardConnection.FillMessageHistory();
        }

        private void ShowNewContactToast(Contact contact)
        {
            string serialized_contact = JsonConvert.SerializeObject(contact);
            var content = new ToastContentBuilder()
                .AddToastActivationInfo("NewContact", ToastActivationType.Foreground)
                .AddText($"{contact.Email}")
                .AddText($"{contact.Email} has added you to their contact list")
                .AddButton("Add to your list", ToastActivationType.Background, new QueryString()
                {
                    {"action", "acceptContact" },
                    {"contact", serialized_contact }
                }.ToString())
                .AddButton("Dismiss", ToastActivationType.Background, new QueryString() 
                {
                    {"action", "Dismiss" }
                }.ToString())
                .GetToastContent();
            try
            {
                var notif = new ToastNotification(content.GetXml())
                {
                    Group = "newContacts"
                };
                ToastNotificationManager.CreateToastNotifier().Show(notif);
            }
            catch (ArgumentException) { }
        }
    }
}
