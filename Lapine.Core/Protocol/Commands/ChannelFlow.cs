namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct ChannelFlow(Boolean Active) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x14);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteBoolean(Active);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ChannelFlow? result) {
        if (BufferExtensions.ReadBoolean(ref buffer, out var active)) {
            result = new ChannelFlow(active);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

readonly record struct ChannelFlowOk(Boolean Active) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x15);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteBoolean(Active);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ChannelFlowOk? result) {
        if (BufferExtensions.ReadBoolean(ref buffer, out var active)) {
            result = new ChannelFlowOk(active);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
