namespace Lapine.Protocol.Commands;

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct ChannelFlow(Boolean Active) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x14);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteBoolean(Active);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ChannelFlow? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadBoolean(out var active, out surplus)) {
            result = new ChannelFlow(active);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

record struct ChannelFlowOk(Boolean Active) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x15);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteBoolean(Active);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ChannelFlowOk? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadBoolean(out var active, out surplus)) {
            result = new ChannelFlowOk(active);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
