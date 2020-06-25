using System;
using System.Collections.Generic;
using System.Text;

namespace AtomicScheduler.Exceptions
{
    class ThreadNotInstantiatedException : Exception
    {
        public ThreadNotInstantiatedException() : base() { }
        public ThreadNotInstantiatedException(string message) : base(message) { }
    }
}
