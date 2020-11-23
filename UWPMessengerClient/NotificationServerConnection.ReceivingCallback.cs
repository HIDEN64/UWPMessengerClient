using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using System.Collections.ObjectModel;

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
            NServerConnection.output_string = Encoding.ASCII.GetString(NServerConnection.received_bytes, 0, bytes_read);
            if (NServerConnection.output_string.Contains("LST "))
            {
                NServerConnection.CreateContactList();
            }
            if (NServerConnection.output_string.Contains("ILN "))
            {
                NServerConnection.SetInitialContactPresence();
            }
            if (NServerConnection.output_string.Contains("PRP MFN"))
            {
                NServerConnection.GetUserDisplayName();
            }
            if (NServerConnection.output_string.StartsWith("NLN "))
            {
                NServerConnection.SetContactPresence();
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
            if (rnIndex != LSTResponses.Last().Length && rnIndex > 0)
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
                    guid = guid.Remove(guid.IndexOf(" "));
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

        public void SetInitialContactPresence()
        {
            string[] ILNResponses = output_string.Split("ILN ");
            //ensuring the last element of the ILNReponses array is just the ILN response
            int rnIndex = ILNResponses.Last().IndexOf("\r\n");
            if (rnIndex != ILNResponses.Last().Length && rnIndex > 0)
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
            if (rnIndex != NLNResponses.Last().Length && rnIndex > 0)
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
            if (rnIndex != FLNResponses.Last().Length && rnIndex > 0)
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
            string[] PRPParams = output_string.Split("PRP MFN ", 2);
            //ensuring the last element of the PRPReponses array is just the PRP response
            int rnIndex = PRPParams.Last().IndexOf("\r\n");
            if (rnIndex != PRPParams.Last().Length && rnIndex > 0)
            {
                PRPParams[PRPParams.Length - 1] = PRPParams.Last().Remove(rnIndex);
            }
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                userInfo.displayName = PRPParams[1];
            });
        }

        public void FillForwardListCollection()
        {
            foreach (Contact contact in contact_list)
            {
                if (contact.onForward == true)
                {
                    contacts_in_forward_list.Add(contact);
                }
            }
        }

        public async Task ConnectToSwitchboard()
        {
            string[] XFRResponse = output_string.Split("XFR ", 2);
            //ensuring the last element of the XFRReponse array is just the XFR response
            int rnIndex = XFRResponse.Last().IndexOf("\r\n");
            if (rnIndex != XFRResponse.Last().Length && rnIndex > 0)
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
            if (rnIndex != RNGResponse.Last().Length && rnIndex > 0)
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
            SwitchboardConnection switchboardConnection = new SwitchboardConnection(sb_address, sb_port, email, trID, userInfo.displayName, sessionID);
            SBConnection = switchboardConnection;
            _ = SBConnection.AnswerRNG();
        }
    }
}
