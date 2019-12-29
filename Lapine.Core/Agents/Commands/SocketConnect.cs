namespace Lapine.Agents.Commands {
    using System;
    using System.Net;

    public class SocketConnect {
        public IPEndPoint Endpoint { get; }

        public SocketConnect(IPEndPoint endpoint) =>
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    }
}
