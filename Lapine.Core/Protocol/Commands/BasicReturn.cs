namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct BasicReturn(UInt16 ReplyCode, String ReplyText, String ExchangeName, String RoutingKey) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x32);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(ReplyCode)
            .WriteShortString(ReplyText)
            .WriteShortString(ExchangeName)
            .WriteShortString(RoutingKey);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out BasicReturn? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out var replyCode) &&
            BufferExtensions.ReadShortString(ref buffer, out var replyText) &&
            BufferExtensions.ReadShortString(ref buffer, out var exchangeName) &&
            BufferExtensions.ReadShortString(ref buffer, out var routingKey))
        {
            result = new BasicReturn(replyCode, replyText, exchangeName, routingKey);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
