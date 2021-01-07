using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace QueryMaster
{
    /// <summary>
    /// Provides methods to create MasterServer instance
    /// </summary>
    public class MasterQuery
    {
        /// <summary>
        /// Master server for Gold Source games
        /// </summary>
        public static IPEndPoint GoldSrcServer = new IPEndPoint(Dns.GetHostAddresses("hl1master.steampowered.com")[0], 27011);
        /// <summary>
        /// Master server for  Source games
        /// </summary>
        public static IPEndPoint SourceServer = new IPEndPoint(Dns.GetHostAddresses("hl2master.steampowered.com")[1], 27011);
        /// <summary>
        /// Gets the appropriate  masterserver query instance
        /// </summary>
        /// <param name="type">Engine used by server</param>
        /// <returns>Master server instance</returns>
        public static MasterServer GetMasterServerInstance(EngineType type)
        {
            MasterServer server = null;
            switch (type)
            {
                case EngineType.GoldSource: server = new MasterServer(GoldSrcServer); break;
                case EngineType.Source: server = new MasterServer(SourceServer); break;
                default: throw new FormatException("An invalid EngineType was specified.");
            }
            return server;
        }
    }
}
