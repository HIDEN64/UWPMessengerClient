using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UWPMessengerClient.MSNP
{
    public class Message : INotifyPropertyChanged
    {
        private string _message_text;
        private string _sender;
        private string _receiver;
        private string _sender_email;
        private string _receiver_email;
        private bool _IsHistory;
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

        public string receiver
        {
            get => _receiver;
            set
            {
                _receiver = value;
                NotifyPropertyChanged();
            }
        }

        public string sender_email
        {
            get => _sender_email;
            set
            {
                _sender_email = value;
                NotifyPropertyChanged();
            }
        }

        public string receiver_email
        {
            get => _receiver_email;
            set
            {
                _receiver_email = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsHistory
        {
            get => _IsHistory;
            set
            {
                _IsHistory = value;
                NotifyPropertyChanged();
            }
        }
    }
}
