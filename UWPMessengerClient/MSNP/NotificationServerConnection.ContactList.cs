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
        private SOAPRequests SOAPRequests;
        private string MembershipLists;
        private string AddressBook;
        public ObservableCollection<Contact> ContactList { get; set; } = new ObservableCollection<Contact>();
        public ObservableCollection<Contact> ContactsInForwardList { get; set; } = new ObservableCollection<Contact>();
        public ObservableCollection<Contact> ContactsInPendingOrReverseList { get; set; } = new ObservableCollection<Contact>();

        protected void GetContactsFromDatabase()
        {
            List<string> JSONContactList = DatabaseAccess.GetUserContacts(userInfo.Email);
            foreach (string JSONContact in JSONContactList)
            {
                Contact contact = JsonConvert.DeserializeObject<Contact>(JSONContact);
                ContactList.Add(contact);
            }
        }

        protected void FillContactListFromSOAP()
        {
            XmlDocument member_list = new XmlDocument();
            member_list.LoadXml(MembershipLists);
            XmlNamespaceManager NSmanager = new XmlNamespaceManager(member_list.NameTable);
            NSmanager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            NSmanager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            NSmanager.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
            NSmanager.AddNamespace("ab", "http://www.msn.com/webservices/AddressBook");
            string xPathString = "//soap:Envelope/soap:Body/ab:FindMembershipResponse/ab:FindMembershipResult/ab:Services/" +
                "ab:Service/ab:Memberships/ab:Membership";
            XmlNodeList memberships = member_list.SelectNodes(xPathString, NSmanager);
            foreach (XmlNode membership in memberships)
            {
                xPathString = "./ab:MemberRole";
                XmlNode member_role = membership.SelectSingleNode(xPathString, NSmanager);
                xPathString = "./ab:Members/ab:Member";
                XmlNodeList members = membership.SelectNodes(xPathString, NSmanager);
                foreach (XmlNode member in members)
                {
                    xPathString = "./ab:PassportName";
                    XmlNode passport_name = member.SelectSingleNode(xPathString, NSmanager);
                    xPathString = "./ab:MembershipId";
                    XmlNode membership_id = member.SelectSingleNode(xPathString, NSmanager);
                    var contactInList = from contact_in_list in ContactList
                                        where contact_in_list.Email == passport_name.InnerText
                                        select contact_in_list;
                    if (!contactInList.Any())
                    {
                        Contact contact = new Contact();
                        contact.Email = passport_name.InnerText;
                        switch (member_role.InnerText)
                        {
                            case "Allow":
                                contact.onAllow = true;
                                contact.AllowMembershipID = membership_id.InnerText;
                                break;
                            case "Block":
                                contact.onBlock = true;
                                contact.BlockMembershipID = membership_id.InnerText;
                                break;
                            case "Reverse":
                                contact.onReverse = true;
                                break;
                            case "Pending":
                                contact.Pending = true;
                                contact.PendingMembershipID = membership_id.InnerText;
                                break;
                        }
                        ContactList.Add(contact);
                        DatabaseAccess.AddContactToTable(userInfo.Email, contact);
                    }
                    else
                    {
                        foreach (Contact list_contact in contactInList)
                        {
                            switch (member_role.InnerText)
                            {
                                case "Allow":
                                    list_contact.onAllow = true;
                                    list_contact.AllowMembershipID = membership_id.InnerText;
                                    break;
                                case "Block":
                                    list_contact.onBlock = true;
                                    list_contact.BlockMembershipID = membership_id.InnerText;
                                    break;
                                case "Reverse":
                                    list_contact.onReverse = true;
                                    break;
                                case "Pending":
                                    list_contact.Pending = true;
                                    list_contact.PendingMembershipID = membership_id.InnerText;
                                    break;
                            }
                        }
                    }
                }
            }
        }

        protected void FillContactsInForwardListFromSOAP()
        {
            XmlDocument address_book_xml = new XmlDocument();
            address_book_xml.LoadXml(AddressBook);
            XmlNamespaceManager NSmanager = new XmlNamespaceManager(address_book_xml.NameTable);
            NSmanager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            NSmanager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            NSmanager.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
            NSmanager.AddNamespace("ab", "http://www.msn.com/webservices/AddressBook");
            string xPath = "//soap:Envelope/soap:Body/ab:ABFindAllResponse/ab:ABFindAllResult/ab:contacts/ab:Contact";
            XmlNodeList contacts = address_book_xml.SelectNodes(xPath, NSmanager);
            foreach (XmlNode contact in contacts)
            {
                xPath = "./ab:contactId";
                XmlNode contactID = contact.SelectSingleNode(xPath, NSmanager);
                xPath = "./ab:contactInfo/ab:contactType";
                XmlNode contactType = contact.SelectSingleNode(xPath, NSmanager);
                switch (contactType.InnerText)
                {
                    case "Me":
                        xPath = "./ab:contactInfo/ab:displayName";
                        XmlNode userDisplayNameNode = contact.SelectSingleNode(xPath, NSmanager);
                        string userDisplayName = PlusCharactersRegex.Replace(userDisplayNameNode.InnerText, "");
                        Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            userInfo.displayName = userDisplayName;
                        });
                        xPath = "./ab:contactInfo/ab:annotations/ab:Annotation[ab:Name='MSN.IM.BLP']/ab:Value";
                        XmlNode BLP_value = contact.SelectSingleNode(xPath, NSmanager);
                        userInfo.BLPValue = BLP_value.InnerText;
                        break;
                    case "Regular":
                        xPath = "./ab:contactInfo/ab:passportName";
                        XmlNode passportName = contact.SelectSingleNode(xPath, NSmanager);
                        xPath = "./ab:contactInfo/ab:displayName";
                        XmlNode displayNameNode = contact.SelectSingleNode(xPath, NSmanager);
                        string displayName = PlusCharactersRegex.Replace(displayNameNode.InnerText, "");
                        var contactInList = from contact_in_list in ContactList
                                            where contact_in_list.Email == passportName.InnerText
                                            select contact_in_list;
                        if (!contactInList.Any())
                        {
                            Contact newContact = new Contact((int)ListNumbers.Forward + (int)ListNumbers.Allow) { displayName = displayName, Email = passportName.InnerText, contactID = contactID.InnerText, onForward = true };
                            ContactList.Add(newContact);
                            DatabaseAccess.AddContactToTable(userInfo.Email, newContact);
                        }
                        else
                        {
                            foreach (Contact contact_in_list in contactInList)
                            {
                                contact_in_list.displayName = displayName;
                                contact_in_list.contactID = contactID.InnerText;
                                contact_in_list.onForward = true;
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
            string contact_payload = @"<ml>";
            foreach (Contact contact in contacts)
            {
                int lisbit = contact.GetListnumberFromForwardAllowBlock();
                if (lisbit > 0)
                {
                    string[] email = contact.Email.Split("@");
                    string name = email[0];
                    string domain = email[1];
                    contact_payload += $@"<d n=""{domain}""><c n=""{name}"" l=""{lisbit}"" t=""1""/></d>";
                }
            }
            contact_payload += @"</ml>";
            return contact_payload;
        }

        public static string ReturnXMLContactPayload(Contact contact)
        {
            string contact_payload = @"<ml>";
            int lisbit = contact.GetListnumberFromForwardAllowBlock();
            if (lisbit > 0)
            {
                string[] email = contact.Email.Split("@");
                string name = email[0];
                string domain = email[1];
                contact_payload += $@"<d n=""{domain}""><c n=""{name}"" l=""{lisbit}"" t=""1""/></d>";
            }
            contact_payload += @"</ml>";
            return contact_payload;
        }

        public static string ReturnXMLNewContactPayload(string newContactEmail, int listnumber = 1)
        {
            if (newContactEmail == "") { throw new ArgumentNullException("Contact email is empty"); }
            string contact_payload = @"<ml>";
            string[] email = newContactEmail.Split("@");
            string name = email[0];
            string domain = email[1];
            contact_payload += $@"<d n=""{domain}""><c n=""{name}"" l=""{listnumber}"" t=""1""/></d>";
            contact_payload += @"</ml>";
            return contact_payload;
        }

        protected void SendBLP()
        {
            string setting = "";
            switch (userInfo.BLPValue)
            {
                case "1":
                    setting = "AL";
                    break;
                case "2":
                    setting = "BL";
                    break;
                //apparently 0 just means null, 1 means AL and 2 means BL
            }
            transactionID++;
            NSSocket.SendCommand($"BLP {transactionID} {setting}\r\n");
        }

        protected void SendInitialADL()
        {
            string contact_payload = ReturnXMLContactPayload(ContactList);
            int payload_length = Encoding.UTF8.GetBytes(contact_payload).Length;
            transactionID++;
            NSSocket.SendCommand($"ADL {transactionID} {payload_length}\r\n{contact_payload}");
        }

        protected void SendUserDisplayName()
        {
            transactionID++;
            NSSocket.SendCommand($"PRP {transactionID} MFN {userInfo.displayName}\r\n");
        }

        public async Task AddNewContact(string newContactEmail, string newContactDisplayName = "")
        {
            if (newContactEmail == "") { throw new ArgumentNullException("Contact email is empty"); }
            if (newContactDisplayName == "") { newContactDisplayName = newContactEmail; }
            await Task.Run(() =>
            {
                switch (MSNPVersion)
                {
                    case "MSNP12":
                        transactionID++;
                        NSSocket.SendCommand($"ADC {transactionID} FL N={newContactEmail} F={newContactDisplayName}\r\n");
                        Windows.Foundation.IAsyncAction adc_task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            Contact newContact = new Contact((int)ListNumbers.Forward + (int)ListNumbers.Allow) { displayName = newContactDisplayName, Email = newContactEmail };
                            ContactList.Add(newContact);
                            ContactsInForwardList.Add(newContact);
                        });
                        break;
                    case "MSNP15":
                        transactionID++;
                        SOAPRequests.ABContactAdd(newContactEmail);
                        string contact_payload = ReturnXMLNewContactPayload(newContactEmail);
                        int payload_length = Encoding.UTF8.GetBytes(contact_payload).Length;
                        NSSocket.SendCommand($"ADL {transactionID} {payload_length}\r\n{contact_payload}");
                        Windows.Foundation.IAsyncAction adl_task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            Contact newContact = new Contact((int)ListNumbers.Forward + (int)ListNumbers.Allow) { displayName = newContactDisplayName, Email = newContactEmail };
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
            if (contactToAccept.onForward) { return; }
            await Task.Run(() =>
            {
                switch (MSNPVersion)
                {
                    case "MSNP12":
                        transactionID++;
                        NSSocket.SendCommand($"ADC {transactionID} FL N={contactToAccept.Email} F={contactToAccept.displayName}\r\n");
                        contactToAccept.onForward = true;
                        Windows.Foundation.IAsyncAction adc_task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            ContactsInForwardList.Add(contactToAccept);
                            ContactsInPendingOrReverseList.Remove(contactToAccept);
                        });
                        break;
                    case "MSNP15":
                        transactionID++;
                        SOAPRequests.ABContactAdd(contactToAccept.Email);
                        string contact_payload = ReturnXMLNewContactPayload(contactToAccept.Email);
                        int payload_length = Encoding.UTF8.GetBytes(contact_payload).Length;
                        NSSocket.SendCommand($"ADL {transactionID} {payload_length}\r\n{contact_payload}");
                        transactionID++;
                        contact_payload = ReturnXMLNewContactPayload(contactToAccept.Email, 2);
                        payload_length = Encoding.UTF8.GetBytes(contact_payload).Length;
                        NSSocket.SendCommand($"ADL {transactionID} {payload_length}\r\n{contact_payload}");
                        contactToAccept.onForward = true;
                        Windows.Foundation.IAsyncAction adl_task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            contactToAccept.displayName = contactToAccept.Email;
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
                switch (MSNPVersion)
                {
                    case "MSNP12":
                        transactionID++;
                        NSSocket.SendCommand($"REM {transactionID} FL {contactToRemove.GUID}\r\n");
                        break;
                    case "MSNP15":
                        transactionID++;
                        SOAPRequests.ABContactDelete(contactToRemove);
                        string contact_payload = ReturnXMLContactPayload(contactToRemove);
                        int payload_length = Encoding.UTF8.GetBytes(contact_payload).Length;
                        NSSocket.SendCommand($"RML {transactionID} {payload_length}\r\n{contact_payload}");
                        break;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
                Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ContactsInForwardList.Remove(contactToRemove);
                    ContactsInPendingOrReverseList.Remove(contactToRemove);
                });
                DatabaseAccess.DeleteContactFromTable(userInfo.Email, contactToRemove);
            });
        }

        public async Task BlockContact(Contact contactToBlock)
        {
            await Task.Run(() =>
            {
                switch (MSNPVersion)
                {
                    case "MSNP12":
                        transactionID++;
                        NSSocket.SendCommand($"ADC {transactionID} BL N={contactToBlock.Email}\r\n");
                        transactionID++;
                        NSSocket.SendCommand($"REM {transactionID} AL {contactToBlock.GUID}\r\n");
                        contactToBlock.onBlock = true;
                        contactToBlock.onAllow = false;
                        break;
                    case "MSNP15":
                        transactionID++;
                        SOAPRequests.BlockContactRequests(contactToBlock);
                        contactToBlock.SetListsFromListnumber((int)ListNumbers.Allow);
                        string contact_payload = ReturnXMLContactPayload(contactToBlock);
                        int payload_length = Encoding.UTF8.GetBytes(contact_payload).Length;
                        NSSocket.SendCommand($"RML {transactionID} {payload_length}\r\n{contact_payload}");
                        transactionID++;
                        contactToBlock.SetListsFromListnumber((int)ListNumbers.Block);
                        contact_payload = ReturnXMLContactPayload(contactToBlock);
                        payload_length = Encoding.UTF8.GetBytes(contact_payload).Length;
                        NSSocket.SendCommand($"ADL {transactionID} {payload_length}\r\n{contact_payload}");
                        break;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
                DatabaseAccess.UpdateContact(userInfo.Email, contactToBlock);
            });
        }

        public async Task UnblockContact(Contact contactToUnblock)
        {
            await Task.Run(() =>
            {
                switch (MSNPVersion)
                {
                    case "MSNP12":
                        transactionID++;
                        NSSocket.SendCommand($"ADC {transactionID} AL N={contactToUnblock.Email}\r\n");
                        transactionID++;
                        NSSocket.SendCommand($"REM {transactionID} BL {contactToUnblock.Email}\r\n");
                        contactToUnblock.onBlock = false;
                        contactToUnblock.onAllow = true;
                        break;
                    case "MSNP15":
                        transactionID++;
                        SOAPRequests.UnblockContactRequests(contactToUnblock);
                        contactToUnblock.SetListsFromListnumber((int)ListNumbers.Block);
                        string contact_payload = ReturnXMLContactPayload(contactToUnblock);
                        int payload_length = Encoding.UTF8.GetBytes(contact_payload).Length;
                        NSSocket.SendCommand($"RML {transactionID} {payload_length}\r\n{contact_payload}");
                        transactionID++;
                        contactToUnblock.SetListsFromListnumber((int)ListNumbers.Allow);
                        contact_payload = ReturnXMLContactPayload(contactToUnblock);
                        payload_length = Encoding.UTF8.GetBytes(contact_payload).Length;
                        NSSocket.SendCommand($"ADL {transactionID} {payload_length}\r\n{contact_payload}");
                        break;
                    default:
                        throw new Exceptions.VersionNotSelectedException();
                }
                DatabaseAccess.UpdateContact(userInfo.Email, contactToUnblock);
            });
        }

        public void FillForwardListCollection()
        {
            foreach (Contact contact in ContactList)
            {
                if (contact.onForward)
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
                if ((contact.onReverse || contact.Pending) && !contact.onForward)
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
