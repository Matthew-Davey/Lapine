namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    public sealed class ConnectionOpen : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x28);

        public String VirtualHost { get; }

        public ConnectionOpen(String virtualHost) =>
            VirtualHost = virtualHost ?? throw new ArgumentNullException(nameof(virtualHost));

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(VirtualHost);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ConnectionOpen result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var vhost, out surplus)) {
                result = new ConnectionOpen(vhost);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    public sealed class ConnectionOpenOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x29);
    }
}
