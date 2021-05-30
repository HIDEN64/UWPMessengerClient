using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UWPMessengerClient.MSNP
{
    public class UserInfo : INotifyPropertyChanged
    {
        private string displayName;
        private string personalMessage;
        private string typingUser;
        public string BlpValue { get; set; }
        public string Email { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        public string PersonalMessage
        {
            get => personalMessage;
            set
            {
                personalMessage = value;
                NotifyPropertyChanged();
            }
        }

        public string UserIsTyping
        {
            get => typingUser;
            set
            {
                typingUser = value;
                NotifyPropertyChanged();
            }
        }
    }
}
