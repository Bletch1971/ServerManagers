using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace QueryMaster
{
    class GoldSource : Server
    {
        internal GoldSource(IPEndPoint address, bool? isObsolete, int sendTimeOut, int receiveTimeOut) : base(address, EngineType.GoldSource, isObsolete, sendTimeOut, receiveTimeOut) { }
        public override Rcon GetControl(string pass)
        {
            RConObj = RconGoldSource.Authorize(socket.Address, pass);
            return RConObj;
        }
    }
}
