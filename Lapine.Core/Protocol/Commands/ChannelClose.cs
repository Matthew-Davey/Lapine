namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct ChannelClose(UInt16 ReplyCode, String ReplyText, (UInt16 ClassId, UInt16 MethodId) FailingMethod) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x28);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(ReplyCode)
            .WriteShortString(ReplyText)
            .WriteUInt16BE(FailingMethod.ClassId)
            .WriteUInt16BE(FailingMethod.MethodId);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ChannelClose? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out var replyCode) &&
            BufferExtensions.ReadShortString(ref buffer, out var replyText) &&
            BufferExtensions.ReadUInt16BE(ref buffer, out var classId) &&
            BufferExtensions.ReadUInt16BE(ref buffer, out var methodId))
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

readonly record struct ChannelCloseOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x29);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ChannelCloseOk? result) {
        result = new ChannelCloseOk();
        return true;
    }
}
