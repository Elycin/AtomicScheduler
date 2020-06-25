using System;
using System.Collections.Generic;
using System.Text;

namespace AtomicScheduler.Drivers
{
    interface IAtomicDriver
    {
        readonly static string Driver;

        void Set(string key);
        bool Has(string key);

    }
}
