namespace Lapine.Protocol.Commands {
    using System;

    public sealed class BasicReturn : ICommand {
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
    }
}
