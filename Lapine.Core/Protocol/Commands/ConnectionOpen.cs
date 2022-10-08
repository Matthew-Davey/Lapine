namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct ConnectionOpen(String VirtualHost) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x28);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteShortString(VirtualHost)
            .WriteShortString(String.Empty) // Deprecated 'capabilities' field...
            .WriteBoolean(false); // Deprecated 'insist' field...

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ConnectionOpen? result) {
        if (BufferExtensions.ReadShortString(ref buffer, out var vhost) &&
            BufferExtensions.ReadShortString(ref buffer, out _) &&
            BufferExtensions.ReadBoolean(ref buffer, out _))
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

readonly record struct ConnectionOpenOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x29);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ConnectionOpenOk? result) {
        result  = new ConnectionOpenOk();
        return true;
    }
}
