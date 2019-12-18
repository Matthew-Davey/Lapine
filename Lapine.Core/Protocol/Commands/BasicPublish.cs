namespace Lapine.Protocol.Commands {
    using System;

    public sealed class BasicPublish : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x28);

        public String ExchangeName { get; }
        public String RoutingKey { get; }
        public Boolean Mandatory { get; }
        public Boolean Immediate { get; }

        public BasicPublish(String exchangeName, String routingKey, Boolean mandatory, Boolean immediate) {
            ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            RoutingKey   = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
            Mandatory    = mandatory;
            Immediate    = immediate;
        }
    }
}
