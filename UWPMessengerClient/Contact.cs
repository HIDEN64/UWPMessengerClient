using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWPMessengerClient
{
    class Contact
    {
        public string email { get; set; }
        public string displayName { get; set; }
        public string GUID { get; set; }
        public string presenceStatus { get; set; }
        public bool onForward, onAllow, onBlock, onReverse, pending;
        public List<string> groupIDs { get; set; }

        public Contact(int listbit)
        {
            SetListsFromListbit(listbit);
        }

        public void SetListsFromListbit(int listbit)
        {
            onForward = (listbit & 1) == 1;
            onAllow = (listbit & 2) == 2;
            onBlock = (listbit & 4) == 4;
            onReverse = (listbit & 8) == 8;
            pending = (listbit & 16) == 16;
        }
    }
}
