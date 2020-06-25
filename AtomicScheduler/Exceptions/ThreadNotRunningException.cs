using System;
using System.Collections.Generic;
using System.Text;

namespace AtomicScheduler.Exceptions
{
    class ThreadNotRunningException : Exception
    {
        public ThreadNotRunningException() : base() { }
        public ThreadNotRunningException(string message) : base(message) { }
    }
}
