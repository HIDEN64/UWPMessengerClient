using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UWPMessengerClient
{
    public class GroupInfoList : List<object>, INotifyPropertyChanged
    {
        public GroupInfoList(IEnumerable<object> items) : base(items) { }
        public event PropertyChangedEventHandler PropertyChanged;
        private object _Key;
        public object Key
        {
            get => _Key;
            set
            {
                _Key = value;
                NotifyPropertyChanged();
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
