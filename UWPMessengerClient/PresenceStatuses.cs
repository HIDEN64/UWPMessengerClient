using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWPMessengerClient
{
    class PresenceStatuses
    {
        //using properties that return presence statuses
        public static string Available
        {
            get { return "NLN"; }
        }

        public static string Busy
        {
            get { return "BSY"; }
        }

        public static string Idle
        {
            get { return "IDL"; }
        }

        public static string BeRightBack
        {
            get { return "BRB"; }
        }

        public static string Away
        {
            get { return "AWY"; }
        }

        public static string OnThePhone
        {
            get { return "PHN"; }
        }

        public static string OutToLunch
        {
            get { return "LUN"; }
        }
    }
}
