using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Core;

namespace UWPMessengerClient.MSNP
{
    public class Contact : INotifyPropertyChanged
    {
        private string email;
        private string displayName;
        private string guid;
        private string contactID;
        private string presenceStatus;
        private string personalMessage;
        public string AllowMembershipID { get; set; }
        public string BlockMembershipID { get; set; }
        public string PendingMembershipID { get; set; }
        public bool OnForward { get; set; }
        public bool OnAllow { get; set; }
        private bool onBlock;
        public bool OnReverse { get; set; }
        public bool Pending { get; set; }
        private List<string> groupIDs;
        public event PropertyChangedEventHandler PropertyChanged;

        public Contact() { }

        public Contact(int listbit)
        {
            SetListsFromListnumber(listbit);
        }

        public void SetListsFromListnumber(int listnumber)
        {
            OnForward = (listnumber & (int)ListNumbers.Forward) == (int)ListNumbers.Forward;
            OnAllow = (listnumber & (int)ListNumbers.Allow) == (int)ListNumbers.Allow;
            onBlock = (listnumber & (int)ListNumbers.Block) == (int)ListNumbers.Block;
            OnReverse = (listnumber & (int)ListNumbers.Reverse) == (int)ListNumbers.Reverse;
            Pending = (listnumber & (int)ListNumbers.Pending) == (int)ListNumbers.Pending;
        }

        public void UpdateListsFromListnumber(int listnumber)
        {
            if ((listnumber & (int)ListNumbers.Forward) == (int)ListNumbers.Forward) { OnForward = true; }
            if ((listnumber & (int)ListNumbers.Allow) == (int)ListNumbers.Allow) { OnAllow = true; }
            if ((listnumber & (int)ListNumbers.Block) == (int)ListNumbers.Block) { onBlock = true; }
            if ((listnumber & (int)ListNumbers.Reverse) == (int)ListNumbers.Reverse) { OnReverse = true; }
            if ((listnumber & (int)ListNumbers.Pending) == (int)ListNumbers.Pending) { Pending = true; }
        }

        public int GetListnumberFromForwardAllowBlock()
        {
            int onForwardInt = OnForward ? (int)ListNumbers.Forward : 0;
            int onAllowInt = OnAllow ? (int)ListNumbers.Allow : 0;
            int onBlockInt = onBlock ? (int)ListNumbers.Block : 0;
            //respective value of each list if true and 0 if false
            int listbit = (onForwardInt & (int)ListNumbers.Forward) + (onAllowInt & (int)ListNumbers.Allow) + (onBlockInt & (int)ListNumbers.Block);
            return listbit;
        }

        public int GetListnumber()
        {
            int onForwardInt = OnForward ? (int)ListNumbers.Forward : 0;
            int onAllowInt = OnAllow ? (int)ListNumbers.Allow : 0;
            int onBlockInt = onBlock ? (int)ListNumbers.Block : 0;
            int onReverseInt = OnReverse ? (int)ListNumbers.Reverse : 0;
            int PendingInt = Pending ? (int)ListNumbers.Pending : 0;
            //respective value of each list if true and 0 if false
            int listbit = (onForwardInt & (int)ListNumbers.Forward) + (onAllowInt & (int)ListNumbers.Allow) + (onBlockInt & (int)ListNumbers.Block) + (onReverseInt & (int)ListNumbers.Reverse) + (PendingInt & (int)ListNumbers.Pending);
            return listbit;
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        public string Email
        {
            get => email;
            set
            {
                email = value;
                NotifyPropertyChanged();
            }
        }

        public string DisplayName
        {
            get => displayName;
            set
            {
                displayName = value;
                NotifyPropertyChanged();
            }
        }

        public string GUID
        {
            get => guid;
            set
            {
                guid = value;
                NotifyPropertyChanged();
            }
        }

        public string ContactID
        {
            get => contactID;
            set
            {
                contactID = value;
                NotifyPropertyChanged();
            }
        }

        public string PresenceStatus
        {
            get => presenceStatus;
            set
            {
                presenceStatus = value;
                NotifyPropertyChanged();
            }
        }

        public string PersonalMessage
        {
            get => personalMessage;
            set
            {
                personalMessage = value;
                NotifyPropertyChanged();
            }
        }

        public bool OnBlock
        {
            get => onBlock;
            set
            {
                onBlock = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> GroupIDs
        {
            get => groupIDs;
            set
            {
                groupIDs = value;
                NotifyPropertyChanged();
            }
        }
    }
}
