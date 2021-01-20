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
        private string _email;
        private string _displayName;
        private string _GUID;
        private string _contactID;
        private string _presenceStatus;
        private string _personalMessage;
        public string MembershipID { get; set; }
        public bool onForward { get; set; }
        public bool onAllow { get; set; }
        private bool _onBlock;
        public bool onReverse { get; set; }
        public bool Pending { get; set; }
        private List<string> _groupIDs;
        public event PropertyChangedEventHandler PropertyChanged;

        public Contact() { }

        public Contact(int listbit)
        {
            SetListsFromListbit(listbit);
        }

        public void SetListsFromListbit(int listbit)
        {
            onForward = (listbit & (int)ListNumbers.Forward) == (int)ListNumbers.Forward;
            onAllow = (listbit & (int)ListNumbers.Allow) == (int)ListNumbers.Allow;
            onBlock = (listbit & (int)ListNumbers.Block) == (int)ListNumbers.Block;
            onReverse = (listbit & (int)ListNumbers.Reverse) == (int)ListNumbers.Reverse;
            Pending = (listbit & (int)ListNumbers.Pending) == (int)ListNumbers.Pending;
        }

        public int GetListbitFromForwardAllowBlock()
        {
            int onForwardInt = onForward ? (int)ListNumbers.Forward : 0;
            int onAllowInt = onAllow ? (int)ListNumbers.Allow : 0;
            int onBlockInt = onBlock ? (int)ListNumbers.Block : 0;
            //respective value of each list if true and 0 if false
            int listbit = (onForwardInt & (int)ListNumbers.Forward) + (onAllowInt & (int)ListNumbers.Allow) + (onBlockInt & (int)ListNumbers.Block);
            return listbit;
        }

        public int GetListbit()
        {
            int onForwardInt = onForward ? (int)ListNumbers.Forward : 0;
            int onAllowInt = onAllow ? (int)ListNumbers.Allow : 0;
            int onBlockInt = onBlock ? (int)ListNumbers.Block : 0;
            int onReverseInt = onReverse ? (int)ListNumbers.Reverse : 0;
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

        public string email
        {
            get => _email;
            set
            {
                _email = value;
                NotifyPropertyChanged();
            }
        }

        public string displayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                NotifyPropertyChanged();
            }
        }

        public string GUID
        {
            get => _GUID;
            set
            {
                _GUID = value;
                NotifyPropertyChanged();
            }
        }

        public string contactID
        {
            get => _contactID;
            set
            {
                _contactID = value;
                NotifyPropertyChanged();
            }
        }

        public string presenceStatus
        {
            get => _presenceStatus;
            set
            {
                _presenceStatus = value;
                NotifyPropertyChanged();
            }
        }

        public string personalMessage
        {
            get => _personalMessage;
            set
            {
                _personalMessage = value;
                NotifyPropertyChanged();
            }
        }

        public bool onBlock
        {
            get => _onBlock;
            set
            {
                _onBlock = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> groupIDs
        {
            get => _groupIDs;
            set
            {
                _groupIDs = value;
                NotifyPropertyChanged();
            }
        }
    }
}
