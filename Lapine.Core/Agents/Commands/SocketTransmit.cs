namespace Lapine.Agents.Commands {
    using System;

    public class SocketTransmit {
        public Byte[] Buffer { get; }

        public SocketTransmit(Byte[] buffer) =>
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }
}
