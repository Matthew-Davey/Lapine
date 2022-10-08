namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct ChannelOpen : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x0A);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteShortString(String.Empty); // reserved_1

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ChannelOpen? result) {
        if (BufferExtensions.ReadShortString(ref buffer, out _)) {
            result = new ChannelOpen();
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

readonly record struct ChannelOpenOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x0B);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteLongString(String.Empty); // reserved_1

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ChannelOpenOk? result) {
        if (BufferExtensions.ReadLongString(ref buffer, out _)) {
            result = new ChannelOpenOk();
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
