using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.UI.Core;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using UWPMessengerClient.MSNP.SOAP;

namespace UWPMessengerClient.MSNP
{
    public partial class NotificationServerConnection
    {
        private SOAPRequests soapRequests;
        private string membershipLists;
        private string addressBook;
        public ObservableCollection<Contact> ContactList { get; set; } = new ObservableCollection<Contact>();
        public ObservableCollection<Contact> ContactsInForwardList { get; set; } = new ObservableCollection<Contact>();
        public ObservableCollection<Contact> ContactsInPendingOrReverseList { get; set; } = new ObservableCollection<Contact>();

        private void GetContactsFromDatabase()
        {
            List<string> jsonContactList = DatabaseAccess.GetUserContacts(UserInfo.Email);
            foreach (string jsonContact in jsonContactList)
            {
                Contact contact = JsonConvert.DeserializeObject<Contact>(jsonContact);
                ContactList.Add(contact);
            }
        }

        private void FillContactListFromSOAP()
        {
            XmlDocument memberList = new XmlDocument();
            memberList.LoadXml(membershipLists);
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(memberList.NameTable);
            namespaceManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            namespaceManager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            namespaceManager.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
            namespaceManager.AddNamespace("ab", "http://www.msn.com/webservices/AddressBook");
            string xPathString = "//soap:Envelope/soap:Body/ab:FindMembershipResponse/ab:FindMembershipResult/ab:Services/" +
                "ab:Service/ab:Memberships/ab:Membership";
            XmlNodeList memberships = memberList.SelectNodes(xPathString, namespaceManager);
            foreach (XmlNode membership in memberships)
            {
                xPathString = "./ab:MemberRole";
                XmlNode memberRole = membership.SelectSingleNode(xPathString, namespaceManager);
                xPathString = "./ab:Members/ab:Member";
                XmlNodeList members = membership.SelectNodes(xPathString, namespaceManager);
                foreach (XmlNode member in members)
                {
                    xPathString = "./ab:PassportName";
                    XmlNode passportName = member.SelectSingleNode(xPathString, namespaceManager);
                    xPathString = "./ab:MembershipId";
                    XmlNode membershipId = member.SelectSingleNode(xPathString, namespaceManager);
                    var contactsInList = from contactInList in ContactList
                                        where contactInList.Email == passportName.InnerText
                                        select contactInList;
                    if (!contactsInList.Any())
                    {
                        Contact contact = new Contact
                        {
                            Email = passportName.InnerText
                        };
                        switch (memberRole.InnerText)
                        {
                            case "Allow":
                                contact.OnAllow = true;
                                contact.AllowMembershipID = membershipId.InnerText;
                                break;
                            case "Block":
                                contact.OnBlock = true;
                                contact.BlockMembershipID = membershipId.InnerText;
                                break;
                            case "Reverse":
                                contact.OnReverse = true;
                                break;
                            case "Pending":
                                contact.Pending = true;
                                contact.PendingMembershipID = membershipId.InnerText;
                                break;
                        }
                        ContactList.Add(contact);
                        DatabaseAccess.AddContactToTable(UserInfo.Email, contact);
                    }
                    else
                    {
                        foreach (Contact listContact in contactsInList)
                        {
                            switch (memberRole.InnerText)
                            {
                                case "Allow":
                                    listContact.OnAllow = true;
                                    listContact.AllowMembershipID = membershipId.InnerText;
                                    break;
                                case "Block":
                                    listContact.OnBlock = true;
                                    listContact.BlockMembershipID = membershipId.InnerText;
                                    break;
                                case "Reverse":
                                    listContact.OnReverse = true;
                                    break;
                                case "Pending":
                                    listContact.Pending = true;
                                    listContact.PendingMembershipID = membershipId.InnerText;
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private void FillContactsInForwardListFromSOAP()
        {
            XmlDocument addressBookXml = new XmlDocument();
            addressBookXml.LoadXml(addressBook);
            XmlNamespaceManager nsManager = new XmlNamespaceManager(addressBookXml.NameTable);
            nsManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsManager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            nsManager.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
            nsManager.AddNamespace("ab", "http://www.msn.com/webservices/AddressBook");
            string xPath = "//soap:Envelope/soap:Body/ab:ABFindAllResponse/ab:ABFindAllResult/ab:contacts/ab:Contact";
            XmlNodeList contacts = addressBookXml.SelectNodes(xPath, nsManager);
            foreach (XmlNode contact in contacts)
            {
                xPath = "./ab:contactId";
                XmlNode contactID = contact.SelectSingleNode(xPath, nsManager);
                xPath = "./ab:contactInfo/ab:contactType";
                XmlNode contactType = contact.SelectSingleNode(xPath, nsManager);
                switch (contactType.InnerText)
                {
                    case "Me":
                        xPath = "./ab:contactInfo/ab:displayName";
                        XmlNode userDisplayNameNode = contact.SelectSingleNode(xPath, nsManager);
                        string userDisplayName = plusCharactersRegex.Replace(userDisplayNameNode.InnerText, "");
                        Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            UserInfo.DisplayName = userDisplayName;
                        });
                        xPath = "./ab:contactInfo/ab:annotations/ab:Annotation[ab:Name='MSN.IM.BLP']/ab:Value";
                        XmlNode blpValue = contact.SelectSingleNode(xPath, nsManager);
                        UserInfo.BlpValue = blpValue.InnerText;
                        break;
                    case "Regular":
                        xPath = "./ab:contactInfo/ab:passportName";
                        XmlNode passportName = contact.SelectSingleNode(xPath, nsManager);
                        xPath = "./ab:contactInfo/ab:displayName";
                        XmlNode displayNameNode = contact.SelectSingleNode(xPath, nsManager);
                        string displayName = plusCharactersRegex.Replace(displayNameNode.InnerText, "");
                        var contactsInList = from contactInList in ContactList
                                            where contactInList.Email == passportName.InnerText
                                            select contactInList;
                        if (!contactsInList.Any())
                        {
                            Contact newContact = new Contact((int)ListNumbers.Forward + (int)ListNumbers.Allow)
                            {
                                DisplayName = displayName,
                                Email = passportName.InnerText,
                                ContactID = contactID.InnerText,
                                OnForward = true
                            };
                            ContactList.Add(newContact);
                            DatabaseAccess.AddContactToTable(UserInfo.Email, newContact);
                        }
                        else
                        {
                            foreach (Contact contactInList in contactsInList)
                            {
                                contactInList.DisplayName = displayName;
                                contactInList.ContactID = contactID.InnerText;
                                contactInList.OnForward = true;
                            }
                        }
                        break;
                }
            }
            FillForwardListCollection();
            FillReverseListCollection();
        }

        public static string ReturnXMLContactPayload(ObservableCollection<Contact> contacts)
        {
            string contactPayload = @"<ml>";
            foreach (Contact contact in contacts)
            {
                int listNumber = contact.GetListNumberFromForwardAllowBlock();
                if (listNumber > 0)
                {
                    string[] email = contact.Email.Split("@");
                    string name = email[0];
                    string domain = email[1];
                    contactPayload += $@"<d n=""{domain}""><c n=""{name}"" l=""{listNumber}"" t=""1""/></d>";
                }
            }
            contactPayload += @"</ml>";
            return contactPayload;
        }

        public static string ReturnXMLContactPayload(Contact contact)
        {
            string contactPayload = @"<ml>";
            int listNumber = contact.GetListNumberFromForwardAllowBlock();
            if (listNumber > 0)
            {
                string[] email = contact.Email.Split("@");
                string name = email[0];
                string domain = email[1];
                contactPayload += $@"<d n=""{domain}""><c n=""{name}"" l=""{listNumber}"" t=""1""/></d>";
            }
            contactPayload += @"</ml>";
            return contactPayload;
        }

        public static string ReturnXMLNewContactPayload(string newContactEmail, int listNumber = 1)
        {
            if (newContactEmail == "") { throw new ArgumentNullException("Contact email is empty"); }
            string contactPayload = @"<ml>";
            string[] email = newContactEmail.Split("@");
            string name = email[0];
            string domain = email[1];
            contactPayload += $@"<d n=""{domain}""><c n=""{name}"" l=""{listNumber}"" t=""1""/></d>";
            contactPayload += @"</ml>";
            return contactPayload;
        }

        private void SendBLP()
        {
            string setting = "";
            switch (UserInfo.BlpValue)
            {
                case "1":
                    setting = "AL";
                    break;
                case "2":
                    setting = "BL";
                    break;
                //apparently 0 just means null, 1 means AL and 2 means BL
            }
            transactionId++;
            nsSocket.SendCommand($"BLP {transactionId} {setting}\r\n");
        }

        private void SendInitialADL()
        {
            string contactPayload = ReturnXMLContactPayload(ContactList);
            int payloadLength = Encoding.UTF8.GetBytes(contactPayload).Length;
            transactionId++;
            nsSocket.SendCommand($"ADL {transactionId} {payloadLength}\r\n{contactPayload}");
        }

        private void SendUserDisplayName()
        {
            transactionId++;
            nsSocket.SendCommand($"PRP {transactionId} MFN {UserInfo.DisplayName}\r\n");
        }

        public async Task AddNewContact(string newContactEmail, string newContactDisplayName = "")
        {
            if (newContactEmail == "") { throw new ArgumentNullException("Contact email is empty"); }
            if (newContactDisplayName == "") { newContactDisplayName = newContactEmail; }
            await Task.Run(() =>
            {
                switch (MsnpVersion)
                {
                    case "MSNP12":
                        transactionId++;
                        nsSocket.SendCommand($"ADC {transactionId} FL N={newContactEmail} F={newContactDisplayName}\r\n");
                        Windows.Foundation.IAsyncAction adcTask = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            Contact newContact = new Contact((int)ListNumbers.Forward + (int)ListNumbers.Allow)
                            {
                                DisplayName = newContactDisplayName,
                                Email = newContactEmail
                            };
                            ContactList.Add(newContact);
                            ContactsInForwardList.Add(newContact);
                        });
                        break;
                    case "MSNP15":
                        transactionId++;
                        soapRequests.AbContactAdd(newContactEmail);
                        string contactPayload = ReturnXMLNewContactPayload(newContactEmail);
                        int payloadLength = Encoding.UTF8.GetBytes(contactPayload).Length;
                        nsSocket.SendCommand($"ADL {transactionId} {payloadLength}\r\n{contactPayload}");
                        Windows.Foundation.IAsyncAction adlTask = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            Contact newContact = new Contact((int)ListNumbers.Forward + (int)ListNumbers.Allow)
                            {
                                DisplayName = newContactDisplayName,
                                Email = newContactEmail
                            };
                            ContactList.Add(newContact);
                            ContactsInForwardList.Add(newContact);
                        });
                        break;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
            });
        }

        public async Task AcceptNewContact(Contact contactToAccept)
        {
            if (contactToAccept == null) { throw new ArgumentNullException("Contact is null"); }
            if (contactToAccept.OnForward) { return; }
            await Task.Run(() =>
            {
                switch (MsnpVersion)
                {
                    case "MSNP12":
                        transactionId++;
                        nsSocket.SendCommand($"ADC {transactionId} FL N={contactToAccept.Email} F={contactToAccept.DisplayName}\r\n");
                        contactToAccept.OnForward = true;
                        Windows.Foundation.IAsyncAction adc_task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            ContactsInForwardList.Add(contactToAccept);
                            ContactsInPendingOrReverseList.Remove(contactToAccept);
                        });
                        break;
                    case "MSNP15":
                        transactionId++;
                        soapRequests.AbContactAdd(contactToAccept.Email);
                        string contactPayload = ReturnXMLNewContactPayload(contactToAccept.Email);
                        int payloadLength = Encoding.UTF8.GetBytes(contactPayload).Length;
                        nsSocket.SendCommand($"ADL {transactionId} {payloadLength}\r\n{contactPayload}");
                        transactionId++;
                        contactPayload = ReturnXMLNewContactPayload(contactToAccept.Email, 2);
                        payloadLength = Encoding.UTF8.GetBytes(contactPayload).Length;
                        nsSocket.SendCommand($"ADL {transactionId} {payloadLength}\r\n{contactPayload}");
                        contactToAccept.OnForward = true;
                        Windows.Foundation.IAsyncAction adl_task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            contactToAccept.DisplayName = contactToAccept.Email;
                            ContactsInForwardList.Add(contactToAccept);
                            ContactsInPendingOrReverseList.Remove(contactToAccept);
                        });
                        break;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
            });
        }

        public async Task RemoveContact(Contact contactToRemove)
        {
            await Task.Run(() =>
            {
                switch (MsnpVersion)
                {
                    case "MSNP12":
                        transactionId++;
                        nsSocket.SendCommand($"REM {transactionId} FL {contactToRemove.GUID}\r\n");
                        break;
                    case "MSNP15":
                        transactionId++;
                        soapRequests.AbContactDelete(contactToRemove);
                        string contactPayload = ReturnXMLContactPayload(contactToRemove);
                        int payloadLength = Encoding.UTF8.GetBytes(contactPayload).Length;
                        nsSocket.SendCommand($"RML {transactionId} {payloadLength}\r\n{contactPayload}");
                        break;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
                Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ContactsInForwardList.Remove(contactToRemove);
                    ContactsInPendingOrReverseList.Remove(contactToRemove);
                });
                DatabaseAccess.DeleteContactFromTable(UserInfo.Email, contactToRemove);
            });
        }

        public async Task BlockContact(Contact contactToBlock)
        {
            await Task.Run(() =>
            {
                switch (MsnpVersion)
                {
                    case "MSNP12":
                        transactionId++;
                        nsSocket.SendCommand($"ADC {transactionId} BL N={contactToBlock.Email}\r\n");
                        transactionId++;
                        nsSocket.SendCommand($"REM {transactionId} AL {contactToBlock.GUID}\r\n");
                        contactToBlock.OnBlock = true;
                        contactToBlock.OnAllow = false;
                        break;
                    case "MSNP15":
                        transactionId++;
                        soapRequests.BlockContactRequests(contactToBlock);
                        contactToBlock.SetListsFromListNumber((int)ListNumbers.Allow);
                        string contactPayload = ReturnXMLContactPayload(contactToBlock);
                        int payloadLength = Encoding.UTF8.GetBytes(contactPayload).Length;
                        nsSocket.SendCommand($"RML {transactionId} {payloadLength}\r\n{contactPayload}");
                        transactionId++;
                        contactToBlock.SetListsFromListNumber((int)ListNumbers.Block);
                        contactPayload = ReturnXMLContactPayload(contactToBlock);
                        payloadLength = Encoding.UTF8.GetBytes(contactPayload).Length;
                        nsSocket.SendCommand($"ADL {transactionId} {payloadLength}\r\n{contactPayload}");
                        break;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
                DatabaseAccess.UpdateContact(UserInfo.Email, contactToBlock);
            });
        }

        public async Task UnblockContact(Contact contactToUnblock)
        {
            await Task.Run(() =>
            {
                switch (MsnpVersion)
                {
                    case "MSNP12":
                        transactionId++;
                        nsSocket.SendCommand($"ADC {transactionId} AL N={contactToUnblock.Email}\r\n");
                        transactionId++;
                        nsSocket.SendCommand($"REM {transactionId} BL {contactToUnblock.Email}\r\n");
                        contactToUnblock.OnBlock = false;
                        contactToUnblock.OnAllow = true;
                        break;
                    case "MSNP15":
                        transactionId++;
                        soapRequests.UnblockContactRequests(contactToUnblock);
                        contactToUnblock.SetListsFromListNumber((int)ListNumbers.Block);
                        string contactPayload = ReturnXMLContactPayload(contactToUnblock);
                        int payloadLength = Encoding.UTF8.GetBytes(contactPayload).Length;
                        nsSocket.SendCommand($"RML {transactionId} {payloadLength}\r\n{contactPayload}");
                        transactionId++;
                        contactToUnblock.SetListsFromListNumber((int)ListNumbers.Allow);
                        contactPayload = ReturnXMLContactPayload(contactToUnblock);
                        payloadLength = Encoding.UTF8.GetBytes(contactPayload).Length;
                        nsSocket.SendCommand($"ADL {transactionId} {payloadLength}\r\n{contactPayload}");
                        break;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
                DatabaseAccess.UpdateContact(UserInfo.Email, contactToUnblock);
            });
        }

        public void FillForwardListCollection()
        {
            foreach (Contact contact in ContactList)
            {
                if (contact.OnForward)
                {
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        ContactsInForwardList.Add(contact);
                    });
                }
            }
        }

        public void FillReverseListCollection()
        {
            foreach (Contact contact in ContactList)
            {
                if ((contact.OnReverse || contact.Pending) && !contact.OnForward)
                {
                    Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        ContactsInPendingOrReverseList.Add(contact);
                    });
                }
            }
        }
    }
}
