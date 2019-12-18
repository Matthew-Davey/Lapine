namespace Lapine.Protocol.Commands {
    using System;

    public sealed class ConnectionOpen : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x28);

        public String VirtualHost { get; }

        public ConnectionOpen(String virtualHost) =>
            VirtualHost = virtualHost ?? throw new ArgumentNullException(nameof(virtualHost));
    }

    public sealed class ConnectionOpenOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x29);
    }
}
