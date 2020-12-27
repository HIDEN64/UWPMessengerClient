using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UWPMessengerClient
{
    public class Contact : INotifyPropertyChanged
    {
        private string _email;
        private string _displayName;
        private string _GUID;
        private string _contactID;
        private string _presenceStatus;
        private string _personalMessage;
        public bool onForward, onAllow, onBlock, onReverse, pending;
        private List<string> _groupIDs;
        public event PropertyChangedEventHandler PropertyChanged;

        public Contact() { }

        public Contact(int listbit)
        {
            SetListsFromListbit(listbit);
        }

        public void SetListsFromListbit(int listbit)
        {
            onForward = (listbit & 1) == 1;
            onAllow = (listbit & 2) == 2;
            onBlock = (listbit & 4) == 4;
            onReverse = (listbit & 8) == 8;
            pending = (listbit & 16) == 16;
        }

        public int GetListbitFromForwardAllowBlock()
        {
            int onForwardInt = onForward ? 1 : 0;
            int onAllowInt = onAllow ? 2 : 0;
            int onBlockInt = onBlock ? 4 : 0;
            //respective value of each list if true and 0 if false
            int listbit = (onForwardInt & 1) + (onAllowInt & 2) + (onBlockInt & 4);
            return listbit;
        }

        public int GetListbit()
        {
            int onForwardInt = onForward ? 1 : 0;
            int onAllowInt = onAllow ? 2 : 0;
            int onBlockInt = onBlock ? 4 : 0;
            int onReverseInt = onReverse ? 8 : 0;
            int PendingInt = pending ? 16 : 0;
            //respective value of each list if true and 0 if false
            int listbit = (onForwardInt & 1) + (onAllowInt & 2) + (onBlockInt & 4) + (onReverseInt & 8) + (PendingInt & 16);
            return listbit;
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
