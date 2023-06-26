namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct ChannelOpen : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x0A);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteShortString(String.Empty); // reserved_1

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ChannelOpen? result) {
        if (buffer.ReadShortString(out var _)) {
            result = new ChannelOpen();
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

record struct ChannelOpenOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x0B);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteLongString(String.Empty); // reserved_1

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ChannelOpenOk? result) {
        if (buffer.ReadLongString(out var _)) {
            result = new ChannelOpenOk();
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
