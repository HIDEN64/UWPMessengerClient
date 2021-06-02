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
            int bytesRead = notificationServerConnection.nsSocket.StopReceiving(asyncResult);
            notificationServerConnection.outputString = Encoding.UTF8.GetString(notificationServerConnection.receivedBytes, 0, bytesRead);
            string[] responses = notificationServerConnection.outputString.Split("\r\n");
            for (var i = 0; i < responses.Length; i++)
            {
                string[] resParameters = responses[i].Split(" ");
                notificationServerConnection.currentResponse = responses[i];
                if (i != responses.Length - 1)
                {
                    notificationServerConnection.nextResponse = responses[i + 1];
                }
                try
                {
                    notificationServerConnection.commandHandlers[resParameters[0]]();
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }
                catch (Exception e)
                {
                    var task = notificationServerConnection.AddToErrorLog($"{resParameters[0]} processing error: " + e.Message);
                }
            }
            if (bytesRead > 0)
            {
                notificationServerConnection.nsSocket.BeginReceiving(notificationServerConnection.receivedBytes, new AsyncCallback(ReceivingCallback), notificationServerConnection);
            }
        }

        private void SeparateAndProcessCommandFromResponse(string response, int payloadSize)
        {
            if (response.Contains("\r\n"))
            {
                response = response.Split("\r\n", 2)[1];
            }
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            byte[] payloadBytes = new byte[payloadSize];
            Buffer.BlockCopy(responseBytes, 0, payloadBytes, 0, payloadSize);
            string payload = Encoding.UTF8.GetString(payloadBytes);
            string newCommand = response.Replace(payload, "");
            if (newCommand != "")
            {
                currentResponse = newCommand;
                string[] commandParameters = newCommand.Split(" ");
                commandHandlers[commandParameters[0]]();
            }
        }

        private string SeparatePayloadFromResponse(string response, int payloadSize)
        {
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            byte[] payloadBytes = new byte[payloadSize];
            Buffer.BlockCopy(responseBytes, 0, payloadBytes, 0, payloadSize);
            string payload = Encoding.UTF8.GetString(payloadBytes);
            return payload;
        }

        private void GetMbiKeyOldNonce()
        {
            string[] usrResponse = outputString.Split("USR ", 2);
            //ensuring the last element of the USRReponse array is just the USR response
            int rnIndex = usrResponse.Last().IndexOf("\r\n");
            if (rnIndex != usrResponse.Last().Length && rnIndex >= 0)
            {
                usrResponse[usrResponse.Length - 1] = usrResponse.Last().Remove(rnIndex);
            }
            string[] usrParameters = usrResponse[1].Split(" ");
            string mbiKeyOld = usrParameters[4];
            mbiKeyOldNonce = mbiKeyOld;
        }

        public void HandleLst()
        {
            string[] lstParameters = currentResponse.Split(" ");
            string email, displayName, guid;
            int listNumber = 0;
            email = lstParameters[1].Replace("N=","");
            displayName = lstParameters[2].Replace("F=", "");
            try
            {
                guid = lstParameters[3].Replace("C=", "");
            }
            catch (IndexOutOfRangeException)
            {
                guid = null;
            }
            int.TryParse(lstParameters[4], out listNumber);
            displayName = plusCharactersRegex.Replace(displayName, "");
            var contactsInList = from contactInList in ContactList
                                where contactInList.Email == email
                                select contactInList;
            if (!contactsInList.Any())
            {
                Contact newContact = new Contact(listNumber)
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
                foreach (Contact contactInList in contactsInList)
                {
                    contactInList.SetListsFromListNumber(listNumber);
                    contactInList.DisplayName = displayName;
                    contactInList.GUID = guid;
                    if (contactInList.OnForward)
                    {
                        Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            ContactsInForwardList.Add(contactInList);
                        });
                    }
                    if (contactInList.OnReverse || contactInList.Pending)
                    {
                        Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            ContactsInPendingOrReverseList.Add(contactInList);
                        });
                    }
                }
            }
        }

        public void HandleAdc()
        {
            string[] adcParameters = currentResponse.Split(" ");
            string email, displayName;
            email = adcParameters[3].Replace("N=", "");
            displayName = adcParameters[4].Replace("F=", "");
            displayName = plusCharactersRegex.Replace(displayName, "");
            if (adcParameters[2] == "RL")
            {
                var contactsInList = from contactInList in ContactList
                                    where contactInList.Email == email
                                    select contactInList;
                if (!contactsInList.Any())
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
                    foreach (Contact contact in contactsInList)
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

        public void HandleAdl()
        {
            string[] adlParameters = currentResponse.Split(" ");
            if (adlParameters[2] == "OK") { return; }
            int payloadLength;
            int.TryParse(adlParameters[2], out payloadLength);
            string payload = SeparatePayloadFromResponse(nextResponse, payloadLength);
            XmlDocument payloadXml = new XmlDocument();
            payloadXml.LoadXml(payload);
            XmlNode dNode = payloadXml.SelectSingleNode("//ml/d");
            XmlNode cNode = payloadXml.SelectSingleNode("//ml/d/c");
            XmlAttribute domainAttribute = dNode.Attributes["n"];
            XmlAttribute emailNameAttribute = cNode.Attributes["n"];
            XmlAttribute displayNameAttribute = cNode.Attributes["f"];
            XmlAttribute listNumberAttribute = cNode.Attributes["l"];
            string email = emailNameAttribute.InnerText + "@" + domainAttribute.InnerText;
            string displayName = displayNameAttribute.InnerText;
            int listNumber;
            int.TryParse(listNumberAttribute.InnerText, out listNumber);
            var contactsInList = from contactInList in ContactList
                                where contactInList.Email == email
                                select contactInList;
            if (!contactsInList.Any())
            {
                Contact newContact = new Contact(listNumber)
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
                foreach (Contact contact in contactsInList)
                {
                    contact.UpdateListsFromListNumber(listNumber);
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

        public void HandlePrp()
        {
            string[] prpParameters = currentResponse.Split(" ");
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (prpParameters[1] == "MFN")
                {
                    string displayName = plusCharactersRegex.Replace(prpParameters[2], "");
                    UserInfo.DisplayName = displayName;
                }
                else if (prpParameters[2] == "MFN")
                {
                    string displayName = plusCharactersRegex.Replace(prpParameters[3], "");
                    UserInfo.DisplayName = displayName;
                }
            });
        }

        public void HandleIln()
        {
            //gets the parameters, does a LINQ query in the contact list and sets the contact's status
            string[] ilnParameters = currentResponse.Split(" ");
            string status = ilnParameters[2];
            string email = ilnParameters[3];
            string displayName = "";
            switch (MsnpVersion)
            {
                case "MSNP12":
                    displayName = ilnParameters[4];
                    break;
                case "MSNP15":
                    displayName = ilnParameters[5];
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
            displayName = plusCharactersRegex.Replace(displayName, "");
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var contactsWithPresence = from contact in ContactList
                                          where contact.Email == email
                                          select contact;
                foreach (Contact contact in contactsWithPresence)
                {
                    contact.PresenceStatus = status;
                    contact.DisplayName = displayName;
                    DatabaseAccess.UpdateContact(UserInfo.Email, contact);
                }
                var contactWithPresenceInForwardList = from contact in ContactsInForwardList
                                          where contact.Email == email
                                          select contact;
                foreach (Contact contact in contactWithPresenceInForwardList)
                {
                    contact.PresenceStatus = status;
                    contact.DisplayName = displayName;
                }
            });
        }

        public void HandleNln()
        {
            //gets the parameters, does a LINQ query in the contact list and sets the contact's status
            string[] nlnParameters = currentResponse.Split(" ");
            string status = nlnParameters[1];
            string email = nlnParameters[2];
            string displayName = "";
            switch (MsnpVersion)
            {
                case "MSNP12":
                    displayName = nlnParameters[3];
                    break;
                case "MSNP15":
                    displayName = nlnParameters[4];
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
            displayName = plusCharactersRegex.Replace(displayName, "");
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var contactsWithPresence = from contact in ContactList
                                          where contact.Email == email
                                          select contact;
                foreach (Contact contact in contactsWithPresence)
                {
                    contact.PresenceStatus = status;
                    contact.DisplayName = displayName;
                    DatabaseAccess.UpdateContact(UserInfo.Email, contact);
                }
                var contactsWithPresenceInForwardList = from contact in ContactsInForwardList
                                                   where contact.Email == email
                                                   select contact;
                foreach (Contact contact in contactsWithPresenceInForwardList)
                {
                    contact.PresenceStatus = status;
                    contact.DisplayName = displayName;
                }
            });
        }

        public void HandleFln()
        {
            string[] flnParameters = currentResponse.Split(" ");
            //for the FLN response gets the email, does a LINQ query in the contact list and sets the contact's status to offline
            string email = flnParameters[1];
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var contactsWithPresence = from contact in ContactList
                                            where contact.Email == email
                                            select contact;
                foreach (Contact contact in contactsWithPresence)
                {
                    contact.PresenceStatus = null;
                }
                var contactsWithPresenceInForwardList = from contact in ContactsInForwardList
                                                   where contact.Email == email
                                                   select contact;
                foreach (Contact contact in contactsWithPresenceInForwardList)
                {
                    contact.PresenceStatus = null;
                }
            });
        }

        public void HandleUbx()
        {
            string personalMessage;
            string[] ubxParameters = currentResponse.Split(" ");
            string principalEmail = ubxParameters[1];
            string lengthString = "";
            switch (MsnpVersion)
            {
                case "MSNP12":
                    lengthString = ubxParameters[2];
                    break;
                case "MSNP15":
                    lengthString = ubxParameters[3];
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
            int ubxLength;
            int.TryParse(lengthString, out ubxLength);
            string payload = SeparatePayloadFromResponse(nextResponse, ubxLength);
            XmlDocument personalMessagePayload = new XmlDocument();
            try
            {
                personalMessagePayload.LoadXml(payload);
                string xPath = "//Data/PSM";
                XmlNode PSM = personalMessagePayload.SelectSingleNode(xPath);
                personalMessage = PSM.InnerText;
            }
            catch (XmlException)
            {
                personalMessage = "XML error";
            }
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var contactsWithPersonalMessage = from contact in ContactList
                                                 where contact.Email == principalEmail
                                                 select contact;
                foreach (Contact contact in contactsWithPersonalMessage)
                {
                    contact.PersonalMessage = personalMessage;
                    DatabaseAccess.UpdateContact(UserInfo.Email, contact);
                }
                var contactWithPersonalMessageInForwardList = from contact in ContactsInForwardList
                                                 where contact.Email == principalEmail
                                                 select contact;
                foreach (Contact contact in contactWithPersonalMessageInForwardList)
                {
                    contact.PersonalMessage = personalMessage;
                }
            });
            SeparateAndProcessCommandFromResponse(nextResponse, ubxLength);
        }

        public async Task HandleXfr()
        {
            string[] xfrParameters = currentResponse.Split(" ");
            string[] addressAndPort = xfrParameters[3].Split(":");
            string sbAddress = addressAndPort[0];
            int sbPort;
            int.TryParse(addressAndPort[1], out sbPort);
            string authString = "";
            switch (MsnpVersion)
            {
                case "MSNP12":
                    authString = xfrParameters.Last();
                    break;
                case "MSNP15":
                    authString = xfrParameters[5];
                    break;
                default:
                    throw new Exceptions.VersionNotSelectedException();
            }
            SwitchboardConnection switchboardConnection = new SwitchboardConnection(sbAddress, sbPort, email, authString, UserInfo.DisplayName)
            {
                KeepMessagingHistory = KeepMessagingHistoryInSwitchboard
            };
            await switchboardConnection.LoginToNewSwitchboardAsync();
            await switchboardConnection.InvitePrincipal(ContactToChat.Email, ContactToChat.DisplayName);
            SwitchboardCreated?.Invoke(this, new SwitchboardEventArgs() { switchboard = switchboardConnection});
            switchboardConnection.FillMessageHistory();
        }

        public void HandleRng()
        {
            string[] rngParameters = currentResponse.Split(" ");
            string sessionId = rngParameters[1];
            string[] addressAndPort = rngParameters[2].Split(":");
            int sbPort;
            string sbAddress = addressAndPort[0];
            int.TryParse(addressAndPort[1], out sbPort);
            string authString = rngParameters[4];
            string principalEmail = rngParameters[5];
            string principalName = rngParameters[6];
            int conversationId = random.Next(1000, 9999);
            SBConversation conversation = new SBConversation(this, Convert.ToString(conversationId));
            SbConversations.Add(conversation);
            SwitchboardConnection switchboardConnection = new SwitchboardConnection(sbAddress, sbPort, email, authString, UserInfo.DisplayName, principalName, principalEmail, sessionId)
            {
                KeepMessagingHistory = KeepMessagingHistoryInSwitchboard
            };
            _ = switchboardConnection.AnswerRNG();
            SwitchboardCreated?.Invoke(this, new SwitchboardEventArgs() { switchboard = switchboardConnection });
            switchboardConnection.FillMessageHistory();
        }

        private void ShowNewContactToast(Contact contact)
        {
            string serializedContact = JsonConvert.SerializeObject(contact);
            var content = new ToastContentBuilder()
                .AddToastActivationInfo("NewContact", ToastActivationType.Foreground)
                .AddText($"{contact.Email}")
                .AddText($"{contact.Email} has added you to their contact list")
                .AddButton("Add to your list", ToastActivationType.Background, new QueryString()
                {
                    {"action", "acceptContact" },
                    {"contact", serializedContact }
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
