using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWPMessengerClient.MSNP.SOAP
{
    partial class SOAPRequests
    {
        protected string SharingService_url = "https://m1.escargot.log1p.xyz/abservice/SharingService.asmx";
        //local address is http://localhost/abservice/SharingService.asmx for SharingService_url

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

        public string MakeBlockContactSOAPRequests(Contact contact)
        {
            string block_contact_xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
                           xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
                           xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
                           xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"">
                <soap:Header>
                    <ABApplicationHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <ApplicationId>996CDE1B-AA53-4477-B943-2BB802EA6166</ApplicationId>
                        <IsMigration>false</IsMigration>
                        <PartnerScenario>BlockUnblock</PartnerScenario>
                    </ABApplicationHeader>
                    <ABAuthHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <ManagedGroupRequest>false</ManagedGroupRequest>
                        <TicketToken>{TicketToken}</TicketToken>
                    </ABAuthHeader>
                </soap:Header>
                <soap:Body>
                    <DeleteMember xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <serviceHandle>
                            <Id>0</Id>
                            <Type>Messenger</Type>
                            <ForeignId></ForeignId>
                        </serviceHandle>
                        <memberships>
                            <Membership>
                                <MemberRole>Allow</MemberRole>
                                <Members>
                                    <Member xsi:type=""PassportMember"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
                                        <Type>Passport</Type>
                                        <MembershipId>{contact.MembershipID}</MembershipId>
                                        <State>Accepted</State>
                                    </Member>
                                </Members>
                            </Membership>
                        </memberships>
                    </DeleteMember>
                </soap:Body>
            </soap:Envelope>";
            if (contact.MembershipID != null)
            {
                MakeSOAPRequest(block_contact_xml, SharingService_url, "http://www.msn.com/webservices/AddressBook/DeleteMember");
            }
            block_contact_xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
                           xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
                           xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
                           xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"">
                <soap:Header>
                    <ABApplicationHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <ApplicationId>996CDE1B-AA53-4477-B943-2BB802EA6166</ApplicationId>
                        <IsMigration>false</IsMigration>
                        <PartnerScenario>BlockUnblock</PartnerScenario>
                    </ABApplicationHeader>
                    <ABAuthHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <ManagedGroupRequest>false</ManagedGroupRequest>
                        <TicketToken>{TicketToken}</TicketToken>
                    </ABAuthHeader>
                </soap:Header>
                <soap:Body>
                    <AddMember xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <serviceHandle>
                            <Id>0</Id>
                            <Type>Messenger</Type>
                            <ForeignId></ForeignId>
                        </serviceHandle>
                        <memberships>
                            <Membership>
                                <MemberRole>Block</MemberRole>
                                <Members>
                                    <Member xsi:type=""PassportMember"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
                                        <Type>Passport</Type>
                                        <State>Accepted</State>
                                        <PassportName>{contact.email}</PassportName>
                                    </Member>
                                </Members>
                            </Membership>
                        </memberships>
                    </AddMember>
                </soap:Body>
            </soap:Envelope>";
            return MakeSOAPRequest(block_contact_xml, SharingService_url, "http://www.msn.com/webservices/AddressBook/AddMember");
        }

        public string MakeUnblockContactSOAPRequests(Contact contact)
        {
            string unblock_contact_xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
                           xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
                           xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
                           xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"">
                <soap:Header>
                    <ABApplicationHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <ApplicationId>996CDE1B-AA53-4477-B943-2BB802EA6166</ApplicationId>
                        <IsMigration>false</IsMigration>
                        <PartnerScenario>BlockUnblock</PartnerScenario>
                    </ABApplicationHeader>
                    <ABAuthHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <ManagedGroupRequest>false</ManagedGroupRequest>
                        <TicketToken>{TicketToken}</TicketToken>
                    </ABAuthHeader>
                </soap:Header>
                <soap:Body>
                    <DeleteMember xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <serviceHandle>
                            <Id>0</Id>
                            <Type>Messenger</Type>
                            <ForeignId></ForeignId>
                        </serviceHandle>
                        <memberships>
                            <Membership>
                                <MemberRole>Block</MemberRole>
                                <Members>
                                    <Member xsi:type=""PassportMember"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
                                        <Type>Passport</Type>
                                        <MembershipId>{contact.MembershipID}</MembershipId>
                                        <State>Accepted</State>
                                    </Member>
                                </Members>
                            </Membership>
                        </memberships>
                    </DeleteMember>
                </soap:Body>
            </soap:Envelope>";
            if (contact.MembershipID != null)
            {
                MakeSOAPRequest(unblock_contact_xml, SharingService_url, "http://www.msn.com/webservices/AddressBook/DeleteMember");
            }
            unblock_contact_xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
                           xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
                           xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
                           xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"">
                <soap:Header>
                    <ABApplicationHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <ApplicationId>996CDE1B-AA53-4477-B943-2BB802EA6166</ApplicationId>
                        <IsMigration>false</IsMigration>
                        <PartnerScenario>BlockUnblock</PartnerScenario>
                    </ABApplicationHeader>
                    <ABAuthHeader xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <ManagedGroupRequest>false</ManagedGroupRequest>
                        <TicketToken>{TicketToken}</TicketToken>
                    </ABAuthHeader>
                </soap:Header>
                <soap:Body>
                    <AddMember xmlns=""http://www.msn.com/webservices/AddressBook"">
                        <serviceHandle>
                            <Id>0</Id>
                            <Type>Messenger</Type>
                            <ForeignId></ForeignId>
                        </serviceHandle>
                        <memberships>
                            <Membership>
                                <MemberRole>Allow</MemberRole>
                                <Members>
                                    <Member xsi:type=""PassportMember"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
                                        <Type>Passport</Type>
                                        <State>Accepted</State>
                                        <PassportName>{contact.email}</PassportName>
                                    </Member>
                                </Members>
                            </Membership>
                        </memberships>
                    </AddMember>
                </soap:Body>
            </soap:Envelope>";
            return MakeSOAPRequest(unblock_contact_xml, SharingService_url, "http://www.msn.com/webservices/AddressBook/AddMember");
        }
    }
}
