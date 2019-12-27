namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    public sealed class BasicReturn : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x32);

        public UInt16 ReplyCode { get; }
        public String ReplyText { get; }
        public String ExchangeName { get; }
        public String RoutingKey { get; }

        public BasicReturn(UInt16 replyCode, String replyText, String exchangeName, String routingKey) {
            ReplyCode    = replyCode;
            ReplyText    = replyText;
            ExchangeName = exchangeName;
            RoutingKey   = routingKey;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt16BE(ReplyCode)
                .WriteShortString(ReplyText)
                .WriteShortString(ExchangeName)
                .WriteShortString(RoutingKey);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out BasicReturn result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt16BE(out var replyCode, out surplus) &&
                surplus.ReadShortString(out var replyText, out surplus) &&
                surplus.ReadShortString(out var exchangeName, out surplus) &&
                surplus.ReadShortString(out var routingKey, out surplus))
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
}
