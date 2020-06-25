using System;
using System.Collections.Generic;
using System.Text;

namespace AtomicScheduler.Exceptions
{
    class AtomicLockMissingConnection : Exception
    {
        public AtomicLockMissingConnection() : base() { }
        public AtomicLockMissingConnection(string message) : base(message) { }
    }
}
