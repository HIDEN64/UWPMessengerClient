using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using UWPMessengerClient.MSNP;

namespace UWPMessengerClient
{
    class StatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string fullStatus;
            string status = (string)value;
            switch (status)
            {
                case PresenceStatuses.Available:
                    fullStatus = "Available";
                    break;
                case PresenceStatuses.Busy:
                    fullStatus = "Busy";
                    break;
                case PresenceStatuses.Away:
                    fullStatus = "Away";
                    break;
                case PresenceStatuses.Idle:
                    fullStatus = "Idle";
                    break;
                case PresenceStatuses.BeRightBack:
                    fullStatus = "Be right back";
                    break;
                case PresenceStatuses.OnThePhone:
                    fullStatus = "On the phone";
                    break;
                case PresenceStatuses.OutToLunch:
                    fullStatus = "Out to lunch";
                    break;
                case null:
                    fullStatus = "Offline";
                    break;
                default:
                    fullStatus = "Invalid status";
                    break;
            }
            return fullStatus;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
