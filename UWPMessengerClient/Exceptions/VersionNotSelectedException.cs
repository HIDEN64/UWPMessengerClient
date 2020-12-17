using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWPMessengerClient.Exceptions
{
    class VersionNotSelectedException : Exception
    {
        public VersionNotSelectedException() { }

        public VersionNotSelectedException(string message) : base(message) { }

        public VersionNotSelectedException(string message, Exception inner) : base(message, inner) { }
    }
}
