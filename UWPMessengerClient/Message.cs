using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UWPMessengerClient
{
    public class Message : INotifyPropertyChanged
    {
        private string _message_text;
        private string _sender;
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string message_text
        {
            get => _message_text;
            set
            {
                _message_text = value;
                NotifyPropertyChanged();
            }
        }

        public string sender
        {
            get => _sender;
            set
            {
                _sender = value;
                NotifyPropertyChanged();
            }
        }
    }
}
