using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace QueryMaster
{
    /// <summary>
    /// Encapsulates a method that has a parameter of type ReadOnlyCollection which accepts IPEndPoint instances.
    /// Invoked when a reply from Master Server is received.
    /// </summary>
    /// <param name="endPoints">Server Sockets</param>
    public delegate void MasterIpCallback(ReadOnlyCollection<IPEndPoint> endPoints);

    /// <summary>
    /// Provides methods to query master server.
    /// </summary>
    public class MasterServer : IDisposable
    {
        public readonly IPEndPoint SeedEndpoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0);
        Socket UdpSocket;
        IPEndPoint RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private bool IsListening = false;
        private MasterIpCallback Callback;
        private Region RegionCode;
        private IpFilter Filter;
        private byte[] Msg;
        private byte[] recvData;
        internal MasterServer(IPEndPoint endPoint)
        {
            UdpSocket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, ProtocolType.Udp);
            UdpSocket.Connect(endPoint);
        }
        /// <summary>
        /// Starts receiving socket addresses of servers.
        /// </summary>
        /// <param name="region">The region of the world that you wish to find servers in.</param>
        /// <param name="callback">Called when a batch of Socket addresses are received.</param>
        /// <param name="filter">Used to set filter on the type of server required.</param>
        public void GetAddresses(Region region, MasterIpCallback callback, IpFilter filter = null)
        {
            if (IsListening) return;

            RegionCode = region;
            Callback = callback;
            Filter = filter;
            IsListening = true;
            IPEndPoint endPoint = SeedEndpoint;
            Msg = MasterUtil.BuildPacket(endPoint.ToString(), RegionCode, Filter);
            UdpSocket.Send(Msg);
            recvData = new byte[1400];
            UdpSocket.BeginReceive(recvData, 0, recvData.Length, SocketFlags.None, recv, null);

        }

        private void recv(IAsyncResult res)
        {
            int bytesRev = 0;
            try
            {
                bytesRev = UdpSocket.EndReceive(res);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            var endpoints = MasterUtil.ProcessPacket(recvData.Take(bytesRev).ToArray());
            //ThreadPool.QueueUserWorkItem(x => Callback(endpoints));
            Callback(endpoints);
            if (!endpoints.Last().Equals(SeedEndpoint))
            {
                Msg = MasterUtil.BuildPacket(endpoints.Last().ToString(), RegionCode, Filter);
                UdpSocket.Send(Msg);
                UdpSocket.BeginReceive(recvData, 0, recvData.Length, SocketFlags.None, recv, null);
            }
            else
            {
                IsListening = false;
            }
        }

        /// <summary>
        /// Disposes all the resources used MasterServer instance
        /// </summary>
        public void Dispose()
        {
            if (UdpSocket != null && IsListening == true)
                UdpSocket.Close();

        }
    }
}
