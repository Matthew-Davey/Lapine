namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct ConnectionClose(UInt16 ReplyCode, String ReplyText, (UInt16 ClassId, UInt16 MethodId) FailingMethod) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x32);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(ReplyCode)
            .WriteShortString(ReplyText)
            .WriteUInt16BE(FailingMethod.ClassId)
            .WriteUInt16BE(FailingMethod.MethodId);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConnectionClose? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt16BE(out var replyCode, out surplus) &&
            surplus.ReadShortString(out var replyText, out surplus) &&
            surplus.ReadUInt16BE(out var failingClassId, out surplus) &&
            surplus.ReadUInt16BE(out var failingMethodId, out surplus))
        {
            result = new ConnectionClose(replyCode, replyText, (failingClassId, failingMethodId));
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

record struct ConnectionCloseOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x33);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConnectionCloseOk? result, out ReadOnlySpan<Byte> surplus) {
        surplus = buffer;
        result  = new ConnectionCloseOk();
        return true;
    }
}
