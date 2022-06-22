using ServerManagerTool.Common.Enums;
using System.Diagnostics;

namespace ServerManagerTool.Lib
{
    public struct ServerStatusUpdate
    {
        public Process Process;
        public WatcherServerStatus Status;
        public QueryMaster.ServerInfo ServerInfo;
        public int OnlinePlayerCount;
    }
}
