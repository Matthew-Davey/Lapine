namespace Lapine.Agents.Events {
    using System;

    public class SocketDataReceived {
        public Byte[] Buffer { get; }

        public SocketDataReceived(Byte[] buffer) =>
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }
}
