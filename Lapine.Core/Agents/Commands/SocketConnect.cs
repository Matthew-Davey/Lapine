namespace Lapine.Agents.Commands {
    using System;
    using System.Net;

    public class SocketConnect {
        public IPAddress IpAddress { get; }
        public Int32 Port { get; }

        public SocketConnect(IPAddress ipAddress, Int32 port) {
            IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            Port      = port;
        }
    }
}
