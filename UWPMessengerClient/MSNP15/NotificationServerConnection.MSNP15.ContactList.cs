using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.UI.Core;
using System.Collections.ObjectModel;

namespace UWPMessengerClient.MSNP15
{
    public partial class NotificationServerConnection
    {
        private string MembershipLists;
        private string AddressBook;
        private string SharingService_url = "https://m1.escargot.log1p.xyz/abservice/SharingService.asmx";
        private string abservice_url = "https://m1.escargot.log1p.xyz/abservice/abservice.asmx";
        //local adresses are http://localhost/abservice/SharingService.asmx for SharingService_url and
        //http://localhost/abservice/abservice.asmx for abservice_url

        public string MakeMembershipListsSOAPRequest()
        {
            string membership_lists_xml = $@"<?xml version=""1.0"" encoding=""utf-8"" ?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
               <soap:Header xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                   <ABApplicationHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                       <ApplicationId xmlns=""http://www.msn.com/webservices/AddressBook"">CFE80F9D-180F-4399-82AB-413F33A1FA11</ApplicationId>
                       <IsMigration xmlns=""http://www.msn.com/webservices/AddressBook"">false</IsMigration>
                       <PartnerScenario xmlns=""http://www.msn.com/webservices/AddressBook"">Initial</PartnerScenario>
                   </ABApplicationHeader>
                   <ABAuthHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                       <ManagedGroupRequest xmlns=""http://www.msn.com/webservices/AddressBook"">false</ManagedGroupRequest>
                       <TicketToken>{TicketToken}</TicketToken>
                   </ABAuthHeader>
               </soap:Header>
               <soap:Body xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                   <FindMembership xmlns=""http://www.msn.com/webservices/AddressBook"">
                       <serviceFilter xmlns=""http://www.msn.com/webservices/AddressBook"">
                           <Types xmlns=""http://www.msn.com/webservices/AddressBook"">
                               <ServiceType xmlns=""http://www.msn.com/webservices/AddressBook"">Messenger</ServiceType>
                               <ServiceType xmlns=""http://www.msn.com/webservices/AddressBook"">Space</ServiceType>
                               <ServiceType xmlns=""http://www.msn.com/webservices/AddressBook"">Profile</ServiceType>
                           </Types>
                       </serviceFilter>
                   </FindMembership>
               </soap:Body>
            </soap:Envelope>";
            return MakeSOAPRequest(membership_lists_xml, SharingService_url, "http://www.msn.com/webservices/AddressBook/FindMembership");
        }

        public string MakeAddressBookSOAPRequest()
        {
            string address_book_xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/""
                           xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
                           xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
                           xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"">
	            <soap:Header>
		            <ABApplicationHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
			            <ApplicationId>CFE80F9D-180F-4399-82AB-413F33A1FA11</ApplicationId>
			            <IsMigration>false</IsMigration>
			            <PartnerScenario>Initial</PartnerScenario>
		            </ABApplicationHeader>
		            <ABAuthHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
			            <ManagedGroupRequest>false</ManagedGroupRequest>
                        <TicketToken>{TicketToken}</TicketToken>
		            </ABAuthHeader>
	            </soap:Header>
	            <soap:Body>
		            <ABFindAll xmlns=""http://www.msn.com/webservices/AddressBook"">
			            <abId>00000000-0000-0000-0000-000000000000</abId>
			            <abView>Full</abView>
			            <deltasOnly>false</deltasOnly>
			            <lastChange>0001-01-01T00:00:00.0000000-08:00</lastChange>
		            </ABFindAll>
	            </soap:Body>
            </soap:Envelope>";
            return MakeSOAPRequest(address_book_xml, abservice_url, "http://www.msn.com/webservices/AddressBook/ABFindAll");
        }

        public string MakeAddContactSOAPRequest(string newContactEmail, string newContactDisplayName = "")
        {
            string add_contact_xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/""
                           xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                           xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                           xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"">
                <soap:Header>
                    <ABApplicationHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <ApplicationId>996CDE1B-AA53-4477-B943-2BB802EA6166</ApplicationId>
                        <IsMigration>false</IsMigration>
                        <PartnerScenario>ContactSave</PartnerScenario>
                    </ABApplicationHeader>
                    <ABAuthHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <ManagedGroupRequest>false</ManagedGroupRequest>
                        <TicketToken>{TicketToken}</TicketToken>
                    </ABAuthHeader>
                </soap:Header>
                <soap:Body>
                    <ABContactAdd xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <abId>00000000-0000-0000-0000-000000000000</abId>
                        <contacts>
                            <Contact xmlns=""http://www.msn.com/webservices/AddressBook"">
                                <contactInfo>
                                    <contactType>LivePending</contactType>
                                    <passportName>{newContactEmail}</passportName>
                                    <isMessengerUser>true</isMessengerUser>
                                    <MessengerMemberInfo>
                                        <DisplayName>{newContactDisplayName}</DisplayName>
                                    </MessengerMemberInfo>
                                </contactInfo>
                            </Contact>
                        </contacts>
                        <options>
                            <EnableAllowListManagement>true</EnableAllowListManagement>
                        </options>
                    </ABContactAdd>
                </soap:Body>
            </soap:Envelope>";
            return MakeSOAPRequest(add_contact_xml, abservice_url, "http://www.msn.com/webservices/AddressBook/ABContactAdd");
        }

        public string MakeRemoveContactSOAPRequest(Contact contact)
        {
            string remove_contact_xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/""
                           xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                           xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                           xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"">
                <soap:Header>
                    <ABApplicationHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <ApplicationId>996CDE1B-AA53-4477-B943-2BB802EA6166</ApplicationId>
                        <IsMigration>false</IsMigration>
                        <PartnerScenario>Timer</PartnerScenario>
                    </ABApplicationHeader>
                    <ABAuthHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <ManagedGroupRequest>false</ManagedGroupRequest>
                        <TicketToken>{TicketToken}</TicketToken>
                    </ABAuthHeader>
                </soap:Header>
                <soap:Body>
                    <ABContactDelete xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <abId>00000000-0000-0000-0000-000000000000</abId>
                        <contacts>
                            <Contact>
                                <contactId>{contact.contactID}</contactId>
                            </Contact>
                        </contacts>
                    </ABContactDelete>
                </soap:Body>
            </soap:Envelope>";
            return MakeSOAPRequest(remove_contact_xml, abservice_url, "http://www.msn.com/webservices/AddressBook/ABContactDelete");
        }

        public void FillContactList()
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
                    var contactInList = from contact_in_list in contact_list
                                        where contact_in_list.email == passport_name.InnerText
                                        select contact_in_list;
                    if (!contactInList.Any())
                    {
                        Contact contact = new Contact();
                        contact.email = passport_name.InnerText;
                        switch (member_role.InnerText)
                        {
                            case "Allow":
                                contact.onAllow = true;
                                break;
                            case "Block":
                                contact.onBlock = true;
                                break;
                            case "Reverse":
                                contact.onReverse = true;
                                break;
                            case "Pending":
                                contact.pending = true;
                                break;
                        }
                        contact_list.Add(contact);
                    }
                    else
                    {
                        foreach (Contact list_contact in contactInList)
                        {
                            switch (member_role.InnerText)
                            {
                                case "Allow":
                                    list_contact.onAllow = true;
                                    break;
                                case "Block":
                                    list_contact.onBlock = true;
                                    break;
                                case "Reverse":
                                    list_contact.onReverse = true;
                                    break;
                                case "Pending":
                                    list_contact.pending = true;
                                    break;
                            }
                        }
                    }
                }
            }
        }

        public void FillContactsInForwardList()
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
                        XmlNode userDisplayName = contact.SelectSingleNode(xPath, NSmanager);
                        Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            userInfo.displayName = userDisplayName.InnerText;
                        });
                        xPath = "./ab:contactInfo/ab:annotations/ab:Annotation[ab:Name='MSN.IM.BLP']/ab:Value";
                        XmlNode BLP_value = contact.SelectSingleNode(xPath, NSmanager);
                        userInfo.BLPValue = BLP_value.InnerText;
                        break;
                    case "Regular":
                        xPath = "./ab:contactInfo/ab:passportName";
                        XmlNode passportName = contact.SelectSingleNode(xPath, NSmanager);
                        xPath = "./ab:contactInfo/ab:displayName";
                        XmlNode displayName = contact.SelectSingleNode(xPath, NSmanager);
                        var contactInList = from contact_in_list in contact_list
                                            where contact_in_list.email == passportName.InnerText
                                            select contact_in_list;
                        if (!contactInList.Any())
                        {
                            contact_list.Add(new Contact() { displayName = displayName.InnerText, email = passportName.InnerText, contactID = contactID.InnerText, onForward = true });
                        }
                        else
                        {
                            foreach (Contact contact_in_list in contactInList)
                            {
                                contact_in_list.displayName = displayName.InnerText;
                                contact_in_list.contactID = contactID.InnerText;
                                contact_in_list.onForward = true;
                            }
                        }
                        break;
                }
            }
            FillForwardListCollection();
        }

        public static string ReturnXMLContactPayload(ObservableCollection<Contact> contacts)
        {
            string contact_payload = @"<ml l=""1"">";
            foreach (Contact contact in contacts)
            {
                int lisbit = contact.GetListbitFromForwardAllowBlock();
                if (lisbit > 0)
                {
                    string[] email = contact.email.Split("@");
                    string name = email[0];
                    string domain = email[1];
                    contact_payload += $@"<d n=""{domain}""><c n=""{name}"" l=""{lisbit}"" t=""1"" /></d>";
                }
            }
            contact_payload += @"</ml>";
            return contact_payload;
        }

        public static string ReturnXMLContactPayload(Contact contact)
        {
            string contact_payload = @"<ml l=""1"">";
            int lisbit = contact.GetListbitFromForwardAllowBlock();
            if (lisbit > 0)
            {
                string[] email = contact.email.Split("@");
                string name = email[0];
                string domain = email[1];
                contact_payload += $@"<d n=""{domain}""><c n=""{name}"" l=""{lisbit}"" t=""1"" /></d>";
            }
            contact_payload += @"</ml>";
            return contact_payload;
        }

        public static string ReturnXMLNewContactPayload(string newContactEmail)
        {
            if (newContactEmail == "") { throw new ArgumentNullException("Contact email is empty"); }
            string contact_payload = @"<ml l=""1"">";
            string[] email = newContactEmail.Split("@");
            string name = email[0];
            string domain = email[1];
            contact_payload += $@"<d n=""{domain}""><c n=""{name}"" l=""1"" t=""1"" /></d>";
            contact_payload += @"</ml>";
            return contact_payload;
        }

        public void SendBLP()
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
            NSSocket.SendCommand($"BLP 5 {setting}\r\n");
        }

        public void SendInitialADL()
        {
            string contact_payload = ReturnXMLContactPayload(contact_list);
            int payload_length = Encoding.UTF8.GetBytes(contact_payload).Length;
            NSSocket.SendCommand($"ADL 6 {payload_length}\r\n");
            NSSocket.SendCommand(contact_payload);
        }

        public void SendUserDisplayName()
        {
            NSSocket.SendCommand($"PRP 7 MFN {userInfo.displayName}\r\n");
        }

        public async Task AddContact(string newContactEmail, string newContactDisplayName = "")
        {
            if (newContactEmail == "") { throw new ArgumentNullException("Contact email is empty"); }
            if (newContactDisplayName == "") { newContactDisplayName = newContactEmail; }
            MakeAddContactSOAPRequest(newContactEmail, newContactDisplayName);
            await Task.Run(() =>
            {
                string contact_payload = ReturnXMLNewContactPayload(newContactEmail);
                int payload_length = Encoding.UTF8.GetBytes(contact_payload).Length;
                NSSocket.SendCommand($"ADL 10 {payload_length}\r\n");
                NSSocket.SendCommand(contact_payload);
                Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    contact_list.Add(new Contact() { displayName = newContactDisplayName, email = newContactEmail, onForward = true });
                    contacts_in_forward_list.Add(new Contact() { displayName = newContactDisplayName, email = newContactEmail, onForward = true });
                });
            });
        }

        public async Task RemoveContact(Contact contactToRemove)
        {
            MakeRemoveContactSOAPRequest(contactToRemove);
            await Task.Run(() =>
            {
                string contact_payload = ReturnXMLContactPayload(contactToRemove);
                int payload_length = Encoding.UTF8.GetBytes(contact_payload).Length;
                NSSocket.SendCommand($"RML 10 {payload_length}\r\n");
                NSSocket.SendCommand(contact_payload);
                Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    contacts_in_forward_list.Remove(contactToRemove);
                });
            });
        }
    }
}
