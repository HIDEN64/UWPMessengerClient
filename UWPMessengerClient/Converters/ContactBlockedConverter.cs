using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UWPMessengerClient
{
    class ContactBlockedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string contactBlocked = (bool)value ? " - Blocked" : "";
            return contactBlocked;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
