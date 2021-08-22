namespace Lapine.Protocol.Commands;

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct ChannelClose(UInt16 ReplyCode, String ReplyText, (UInt16 ClassId, UInt16 MethodId) FailingMethod) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x28);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(ReplyCode)
            .WriteShortString(ReplyText)
            .WriteUInt16BE(FailingMethod.ClassId)
            .WriteUInt16BE(FailingMethod.MethodId);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ChannelClose? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt16BE(out var replyCode, out surplus) &&
            surplus.ReadShortString(out var replyText, out surplus) &&
            surplus.ReadUInt16BE(out var classId, out surplus) &&
            surplus.ReadUInt16BE(out var methodId, out surplus))
        {
            result = new ChannelClose(replyCode, replyText, (classId, methodId));
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

record struct ChannelCloseOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x29);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer;

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ChannelCloseOk? result, out ReadOnlySpan<Byte> surplus) {
        surplus = buffer;
        result = new ChannelCloseOk();
        return true;
    }
}
