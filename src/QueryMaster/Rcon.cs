using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryMaster
{
    /// <summary>
    /// Provides methods to access server using rcon password.
    /// </summary>
    public abstract class Rcon : IDisposable
    {
        /// <summary>
        /// Send a Command to server.
        /// </summary>
        /// <param name="cmd">Server command.</param>
        /// <returns>Reply from server(string).</returns>
        public abstract string SendCommand(string cmd);
        /// <summary>
        /// Add a client socket to server's logaddress list.
        /// </summary>
        /// <param name="ip">IP-Address of client.</param>
        /// <param name="port">Port number of client.</param>
        public abstract void AddlogAddress(string ip, ushort port);

        /// <summary>
        /// Delete a client socket to server's logaddress list.
        /// </summary>
        /// <param name="ip">IP-Address of client.</param>
        /// <param name="port">Port number of client.</param>
        public abstract void RemovelogAddress(string ip, ushort port);
        /// <summary>
        /// Disposes rcon Object
        /// </summary>
        public abstract void Dispose();
    }
}
