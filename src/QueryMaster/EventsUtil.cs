using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryMaster
{
    internal static class EventsUtil
    {
        internal static void Fire<T>(this EventHandler<T> handler, object sender, T eventArgs) where T : EventArgs
        {
            if (handler != null)
                handler(sender, eventArgs);
        }
    }
}