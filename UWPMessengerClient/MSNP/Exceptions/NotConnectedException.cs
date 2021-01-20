using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWPMessengerClient.MSNP.Exceptions
{
    class NotConnectedException : Exception
    {
        public NotConnectedException() { }

        public NotConnectedException(string message) : base(message) { }

        public NotConnectedException(string message, Exception inner) : base(message, inner) { }
    }
}
