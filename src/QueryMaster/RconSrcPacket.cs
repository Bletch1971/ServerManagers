using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryMaster
{
    class RconSrcPacket
    {
        internal int Size { get; set; }
        internal int Id { get; set; }
        internal int Type { get; set; }
        internal string Body { get; set; }
    }
}
