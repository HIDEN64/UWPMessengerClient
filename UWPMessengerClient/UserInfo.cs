﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UWPMessengerClient
{
    public class UserInfo : INotifyPropertyChanged
    {
        private string _displayName;
        private static string _personalMessage;
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        public string personalMessage
        {
            get => _personalMessage;
            set
            {
                _personalMessage = value;
                NotifyPropertyChanged();
            }
        }
    }
}
