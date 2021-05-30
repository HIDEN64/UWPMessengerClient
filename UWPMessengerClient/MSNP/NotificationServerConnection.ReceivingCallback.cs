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
        private string soapResult;
        private string ssoTicket;
        private byte[] receivedBytes = new byte[4096];
        private string outputString;
        private string currentResponse;
        private string nextResponse;
        public event EventHandler<SwitchboardEventArgs> SwitchboardCreated;

        public void ReceivingCallback(IAsyncResult asyncResult)
        {
            NotificationServerConnection notificationServerConnection = (NotificationServerConnection)asyncResult.AsyncState;
            int bytesRead = notificationServerConnection.NsSocket.StopReceiving(asyncResult);
            notificationServerConnection.outputString = Encoding.UTF8.GetString(notificationServerConnection.receivedBytes, 0, bytesRead);
            string[] responses = notificationServerConnection.outputString.Split("\r\n");
            for (var i = 0; i < responses.Length; i++)
            {
                string[] resParams = responses[i].Split(" ");
                notificationServerConnection.currentResponse = responses[i];
                if (i != responses.Length - 1)
                {
                    notificationServerConnection.nextResponse = responses[i + 1];
                }
                try
                {
                    notificationServerConnection.CommandHandlers[resParams[0]]();
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }
                catch (Exception e)
                {
                    var task = notificationServerConnection.AddToErrorLog($"{resParams[0]} processing error: " + e.Message);
                }
            }
            if (bytesRead > 0)
            {
                notificationServerConnection.NsSocket.BeginReceiving(notificationServerConnection.receivedBytes, new AsyncCallback(ReceivingCallback), notificationServerConnection);
            }
        }

        protected void SeparateAndProcessCommandFromResponse(string response, int payloadSize)
        {
            if (response.Contains("\r\n"))
            {
                response = response.Split("\r\n", 2)[1];
            }
            byte[] response_bytes = Encoding.UTF8.GetBytes(response);
            byte[] payload_bytes = new byte[payloadSize];
            Buffer.BlockCopy(response_bytes, 0, payload_bytes, 0, payloadSize);
            string payload = Encoding.UTF8.GetString(payload_bytes);
            string new_command = response.Replace(payload, "");
            if (new_command != "")
            {
                currentResponse = new_command;
                string[] cmd_params = new_command.Split(" ");
                CommandHandlers[cmd_params[0]]();
            }
        }

        protected string SeparatePayloadFromResponse(string response, int payloadSize)
        {
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            byte[] payloadBytes = new byte[payloadSize];
            Buffer.BlockCopy(responseBytes, 0, payloadBytes, 0, payloadSize);
            string payload = Encoding.UTF8.GetString(payloadBytes);
            return payload;
        }

        protected void GetMbiKeyOldNonce()
        {
            string[] usrResponse = outputString.Split("USR ", 2);
            //ensuring the last element of the USRReponse array is just the USR response
            int rnIndex = usrResponse.Last().IndexOf("\r\n");
            if (rnIndex != usrResponse.Last().Length && rnIndex >= 0)
            {
                usrResponse[usrResponse.Length - 1] = usrResponse.Last().Remove(rnIndex);
            }
            string[] usrParams = usrResponse[1].Split(" ");
            string mbiKeyOld = usrParams[4];
            mbiKeyOldNonce = mbiKeyOld;
        }

        public void HandleLST()
        {
            string[] LSTParams = currentResponse.Split(" ");
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
            displayName = plusCharactersRegex.Replace(displayName, "");
            var contactInList = from contact_in_list in ContactList
                                where contact_in_list.Email == email
                                select contact_in_list;
            if (!contactInList.Any())
            {
                Contact newContact = new Contact(listnumber)
                {
                    DisplayName = displayName,
                    Email = email,
                    GUID = guid
                };
                ContactList.Add(newContact);
                DatabaseAccess.AddContactToTable(UserInfo.Email, newContact);
                if (newContact.OnForward)
                {
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        ContactsInForwardList.Add(newContact);
                    });
                }
                if (newContact.OnReverse || newContact.Pending)
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
                    contact_in_list.DisplayName = displayName;
                    contact_in_list.GUID = guid;
                    if (contact_in_list.OnForward)
                    {
                        Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            ContactsInForwardList.Add(contact_in_list);
                        });
                    }
                    if (contact_in_list.OnReverse || contact_in_list.Pending)
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
            string[] ADCParams = currentResponse.Split(" ");
            string email, displayName;
            email = ADCParams[3].Replace("N=", "");
            displayName = ADCParams[4].Replace("F=", "");
            displayName = plusCharactersRegex.Replace(displayName, "");
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
                        DisplayName = displayName
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
                        contact.OnReverse = true;
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
            string[] ADLParams = currentResponse.Split(" ");
            if (ADLParams[2] == "OK") { return; }
            int payload_length;
            int.TryParse(ADLParams[2], out payload_length);
            string payload = SeparatePayloadFromResponse(nextResponse, payload_length);
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
                    DisplayName = displayName
                };
                ContactList.Add(newContact);
                if ((newContact.OnReverse || newContact.Pending) && !newContact.OnForward)
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
                    if ((contact.OnReverse || contact.Pending) && !contact.OnForward)
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
            string[] PRPParams = currentResponse.Split(" ");
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (PRPParams[1] == "MFN")
                {
                    string displayName = plusCharactersRegex.Replace(PRPParams[2], "");
                    UserInfo.displayName = displayName;
                }
                else if (PRPParams[2] == "MFN")
                {
                    string displayName = plusCharactersRegex.Replace(PRPParams[3], "");
                    UserInfo.displayName = displayName;
                }
            });
        }

        public void HandleILN()
        {
            //gets the parameters, does a LINQ query in the contact list and sets the contact's status
            string[] ILNParams = currentResponse.Split(" ");
            string status = ILNParams[2];
            string email = ILNParams[3];
            string displayName = "";
            switch (MsnpVersion)
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
            displayName = plusCharactersRegex.Replace(displayName, "");
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var contactWithPresence = from contact in ContactList
                                          where contact.Email == email
                                          select contact;
                foreach (Contact contact in contactWithPresence)
                {
                    contact.PresenceStatus = status;
                    contact.DisplayName = displayName;
                    DatabaseAccess.UpdateContact(UserInfo.Email, contact);
                }
                var contactWithPresenceInForward = from contact in ContactsInForwardList
                                          where contact.Email == email
                                          select contact;
                foreach (Contact contact in contactWithPresenceInForward)
                {
                    contact.PresenceStatus = status;
                    contact.DisplayName = displayName;
                }
            });
        }

        public void HandleNLN()
        {
            //gets the parameters, does a LINQ query in the contact list and sets the contact's status
            string[] NLNParams = currentResponse.Split(" ");
            string status = NLNParams[1];
            string email = NLNParams[2];
            string displayName = "";
            switch (MsnpVersion)
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
            displayName = plusCharactersRegex.Replace(displayName, "");
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var contactWithPresence = from contact in ContactList
                                          where contact.Email == email
                                          select contact;
                foreach (Contact contact in contactWithPresence)
                {
                    contact.PresenceStatus = status;
                    contact.DisplayName = displayName;
                    DatabaseAccess.UpdateContact(UserInfo.Email, contact);
                }
                var contactWithPresenceInForward = from contact in ContactsInForwardList
                                                   where contact.Email == email
                                                   select contact;
                foreach (Contact contact in contactWithPresenceInForward)
                {
                    contact.PresenceStatus = status;
                    contact.DisplayName = displayName;
                }
            });
        }

        public void HandleFLN()
        {
            string[] FLNParams = currentResponse.Split(" ");
            //for the FLN response gets the email, does a LINQ query in the contact list and sets the contact's status to offline
            string email = FLNParams[1];
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var contactWithPresence = from contact in ContactList
                                            where contact.Email == email
                                            select contact;
                foreach (Contact contact in contactWithPresence)
                {
                    contact.PresenceStatus = null;
                }
                var contactWithPresenceInForward = from contact in ContactsInForwardList
                                                   where contact.Email == email
                                                   select contact;
                foreach (Contact contact in contactWithPresenceInForward)
                {
                    contact.PresenceStatus = null;
                }
            });
        }

        public void HandleUBX()
        {
            string personal_message;
            string[] UBXParams = currentResponse.Split(" ");
            string principal_email = UBXParams[1];
            string length_str = "";
            switch (MsnpVersion)
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
            string payload = SeparatePayloadFromResponse(nextResponse, ubx_length);
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
                    contact.PersonalMessage = personal_message;
                    DatabaseAccess.UpdateContact(UserInfo.Email, contact);
                }
                var contactWithPersonalMessageInForward = from contact in ContactsInForwardList
                                                 where contact.Email == principal_email
                                                 select contact;
                foreach (Contact contact in contactWithPersonalMessageInForward)
                {
                    contact.PersonalMessage = personal_message;
                }
            });
            SeparateAndProcessCommandFromResponse(nextResponse, ubx_length);
        }

        public async Task HandleXFR()
        {
            string[] XFRParams = currentResponse.Split(" ");
            string[] address_and_port = XFRParams[3].Split(":");
            string sb_address = address_and_port[0];
            int sb_port;
            int.TryParse(address_and_port[1], out sb_port);
            string auth_string = "";
            switch (MsnpVersion)
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
            SwitchboardConnection switchboardConnection = new SwitchboardConnection(sb_address, sb_port, email, auth_string, UserInfo.displayName)
            {
                KeepMessagingHistory = KeepMessagingHistoryInSwitchboard
            };
            await switchboardConnection.LoginToNewSwitchboardAsync();
            await switchboardConnection.InvitePrincipal(contactToChat.Email, contactToChat.DisplayName);
            SwitchboardCreated?.Invoke(this, new SwitchboardEventArgs() { switchboard = switchboardConnection});
            switchboardConnection.FillMessageHistory();
        }

        public void HandleRNG()
        {
            string[] RNGParams = currentResponse.Split(" ");
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
            SbConversations.Add(conversation);
            SwitchboardConnection switchboardConnection = new SwitchboardConnection(sb_address, sb_port, email, authString, UserInfo.displayName, principalName, principalEmail, sessionID)
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
