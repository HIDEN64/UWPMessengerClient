using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UWPMessengerClient
{
    class Contact : INotifyPropertyChanged
    {
        private string _email;
        private string _displayName;
        private string _GUID;
        private string _presenceStatus;
        public bool onForward, onAllow, onBlock, onReverse, pending;
        private List<string> _groupIDs;
        public event PropertyChangedEventHandler PropertyChanged;

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

        public string presenceStatus
        {
            get => _presenceStatus;
            set
            {
                _presenceStatus = value;
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
