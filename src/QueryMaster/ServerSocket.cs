using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace QueryMaster
{
    internal class ServerSocket : IDisposable
    {
        internal static readonly int UdpBufferSize = 1400;
        internal static readonly int TcpBufferSize = 4110;
        internal Socket socket { set; get; }
        protected internal int BufferSize = 0;
        internal IPEndPoint Address = null;
        protected bool IsDisposed;
        internal ServerSocket(SocketType type)
        {
            switch (type)
            {
                case SocketType.Tcp: socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp); BufferSize = TcpBufferSize; break;
                case SocketType.Udp: socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, ProtocolType.Udp); BufferSize = UdpBufferSize; break;
                default: throw new ArgumentException("An invalid SocketType was specified.");
            }
            socket.SendTimeout = 3000;
            socket.ReceiveTimeout = 3000;

            IsDisposed = false;
        }

        internal void Connect(IPEndPoint address)
        {
            Address = address;
            socket.Connect(Address);
        }

        internal int SendData(byte[] data)
        {
            return socket.Send(data);
        }

        internal byte[] ReceiveData()
        {
            byte[] recvData = new byte[BufferSize];
            int recv = 0;
            recv = socket.Receive(recvData);
            return recvData.Take(recv).ToArray();
        }

        public virtual void Dispose()
        {
            if (IsDisposed)
                return;
            if (socket != null)
                socket.Close();
            IsDisposed = true;
        }
    }
}
