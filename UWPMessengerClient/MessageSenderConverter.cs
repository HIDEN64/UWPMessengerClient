using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Windows.UI.Xaml.Data;

namespace UWPMessengerClient
{
    class MessageSenderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string name = (string)value;
            string nameSays = HttpUtility.UrlDecode(name) + " says:";
            return nameSays;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
