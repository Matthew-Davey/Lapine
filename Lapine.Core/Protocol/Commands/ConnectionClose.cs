namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct ConnectionClose(UInt16 ReplyCode, String ReplyText, (UInt16 ClassId, UInt16 MethodId) FailingMethod) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x32);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(ReplyCode)
            .WriteShortString(ReplyText)
            .WriteUInt16BE(FailingMethod.ClassId)
            .WriteUInt16BE(FailingMethod.MethodId);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ConnectionClose? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out var replyCode) &&
            BufferExtensions.ReadShortString(ref buffer, out var replyText) &&
            BufferExtensions.ReadUInt16BE(ref buffer, out var failingClassId) &&
            BufferExtensions.ReadUInt16BE(ref buffer, out var failingMethodId))
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

readonly record struct ConnectionCloseOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x33);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ConnectionCloseOk? result) {
        result  = new ConnectionCloseOk();
        return true;
    }
}
