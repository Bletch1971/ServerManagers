using System.Net;

namespace QueryMaster
{
    class Source : Server
    {
        internal Source(IPEndPoint address, int sendTimeOut, int receiveTimeOut) : base(address, EngineType.Source, false, sendTimeOut, receiveTimeOut) { }
        public override Rcon GetControl(string pass)
        {
            RConObj = RconSource.Authorize(socket.Address, pass, socket.socket.SendTimeout, socket.socket.ReceiveTimeout);
            return RConObj;
        }
    }
}
