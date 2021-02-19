namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Diagnostics.CodeAnalysis;

    sealed class ConnectionOpen : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x28);

        public String VirtualHost { get; }

        public ConnectionOpen(String virtualHost) =>
            VirtualHost = virtualHost ?? throw new ArgumentNullException(nameof(virtualHost));

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(VirtualHost)
                .WriteShortString(String.Empty) // Deprecated 'capabilities' field...
                .WriteBoolean(false); // Deprecated 'insist' field...

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConnectionOpen? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var vhost, out surplus) &&
                surplus.ReadShortString(out var _, out surplus) &&
                surplus.ReadBoolean(out var _, out surplus))
            {
                result = new ConnectionOpen(vhost);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    sealed class ConnectionOpenOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x29);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConnectionOpenOk? result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result  = new ConnectionOpenOk();
            return true;
        }
    }
}
