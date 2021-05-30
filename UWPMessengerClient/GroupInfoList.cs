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
        private object key;
        public object Key
        {
            get => key;
            set
            {
                key = value;
                NotifyPropertyChanged();
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
