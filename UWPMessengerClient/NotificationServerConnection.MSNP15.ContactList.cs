using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWPMessengerClient
{
    public partial class NotificationServerConnection
    {
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
            return MakeSOAPRequest(membership_lists_xml, "https://m1.escargot.log1p.xyz/abservice/SharingService.asmx", "http://www.msn.com/webservices/AddressBook/FindMembership");
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
            return MakeSOAPRequest(address_book_xml, "https://m1.escargot.log1p.xyz/abservice/abservice.asmx", "http://www.msn.com/webservices/AddressBook/ABFindAll");
        }
    }
}
