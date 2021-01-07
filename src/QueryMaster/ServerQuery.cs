using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace QueryMaster
{
    /// <summary>
    /// Provides methods to create Server instance
    /// </summary>
    public static class ServerQuery
    {
        /// <summary>
        /// Returns an object that represents the server
        /// </summary>
        /// <param name="type">Base engine which game uses</param>
        /// <param name="ip">IP-Address of server</param>
        /// <param name="port">Port number of server</param>
        /// <param name="isObsolete">Obsolete Gold Source servers reply only to half life protocol.if set to true then it would use half life protocol.If set to null,then protocol is identified at runtime[Default : false]</param>
        /// <param name="sendTimeOut">Sets Socket's SendTimeout Property.</param>
        /// <param name="receiveTimeOut">Sets Socket's ReceiveTimeout.</param>
        /// <returns>Instance of server class that represents the connected server.</returns>
        public static Server GetServerInstance(EngineType type, string ip, ushort port, bool? isObsolete = false, int sendTimeOut = 3000, int receiveTimeOut = 3000)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), (int)port);
            return GetServerInstance(type, endPoint, isObsolete, sendTimeOut, receiveTimeOut);
        }

        /// <summary>
        /// Returns an object that represents the server
        /// </summary>
        /// <param name="type">Base engine which game uses</param>
        /// <param name="endPoint">Socket address of server</param>
        /// <param name="isObsolete">Obsolete Gold Source servers reply only to half life protocol.if set to true then it would use half life protocol.If set to null,then protocol is identified at runtime.</param>
        /// <param name="sendTimeOut">Sets Socket's SendTimeout Property.</param>
        /// <param name="receiveTimeOut">Sets Socket's ReceiveTimeout.</param>
        /// <returns>Instance of server class that represents the connected server</returns>
        public static Server GetServerInstance(EngineType type, IPEndPoint endPoint, bool? isObsolete = false, int sendTimeOut = 3000, int receiveTimeOut = 3000)
        {
            Server server = null;
            switch (type)
            {
                case EngineType.GoldSource: server = new GoldSource(endPoint, isObsolete, sendTimeOut, receiveTimeOut); break;
                case EngineType.Source: server = new Source(endPoint, sendTimeOut, receiveTimeOut); break;
                default: throw new ArgumentException("An invalid EngineType was specified.");
            }
            return server;
        }

        /// <summary>
        /// Returns an object that represents the server
        /// </summary>
        /// <param name="game">Name of game</param>
        /// <param name="endPoint">Socket address of server</param>
        /// <param name="isObsolete">Obsolete Gold Source servers reply only to half life protocol.if set to true then it would use half life protocol.If set to null,then protocol is identified at runtime.</param>
        /// <param name="sendTimeOut">Sets Socket's SendTimeout Property.</param>
        /// <param name="receiveTimeOut">Sets Socket's ReceiveTimeout.</param>
        /// <returns>Instance of server class that represents the connected server</returns>
        public static Server GetServerInstance(Game game, IPEndPoint endPoint, bool? isObsolete = false, int sendTimeOut = 3000, int receiveTimeOut = 3000)
        {
            if ((int)game <= 130)
                return GetServerInstance(EngineType.GoldSource, endPoint, isObsolete, sendTimeOut, receiveTimeOut);
            else
                return GetServerInstance(EngineType.Source, endPoint, isObsolete, sendTimeOut, receiveTimeOut);
        }

        /// <summary>
        /// Returns an object that represents the server
        /// </summary>
        /// <param name="game">Name of game</param>
        /// <param name="ip">IP-Address of server</param>
        /// <param name="port">Port number of server</param>
        /// <param name="isObsolete">Obsolete Gold Source servers reply only to half life protocol.if set to true then it would use half life protocol.If set to null,then protocol is identified at runtime.</param>
        /// <param name="sendTimeOut">Sets Socket's SendTimeout Property.</param>
        /// <param name="receiveTimeOut">Sets Socket's ReceiveTimeout.</param>
        /// <returns>Instance of server class that represents the connected server</returns>
        public static Server GetServerInstance(Game game, string ip, ushort port, bool? isObsolete = false, int sendTimeOut = 3000, int receiveTimeOut = 3000)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), (int)port);
            if ((int)game <= 130)
                return GetServerInstance(EngineType.GoldSource, endPoint, isObsolete, sendTimeOut, receiveTimeOut);
            else
                return GetServerInstance(EngineType.Source, endPoint, isObsolete, sendTimeOut, receiveTimeOut);
        }

    }
}
