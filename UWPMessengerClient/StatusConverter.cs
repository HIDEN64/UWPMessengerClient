using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

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
                case "NLN":
                    fullStatus = "Available";
                    break;
                case "BSY":
                    fullStatus = "Busy";
                    break;
                case "AWY":
                    fullStatus = "Away";
                    break;
                case "IDL":
                    fullStatus = "Idle";
                    break;
                case "BRB":
                    fullStatus = "Be right back";
                    break;
                case "PHN":
                    fullStatus = "On the phone";
                    break;
                case "LUN":
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

        public Object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
