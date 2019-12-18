namespace Lapine.Protocol.Commands {
    using System;

    public sealed class BasicGet : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x46);

        public String QueueName { get; }
        public Boolean NoAck { get; }

        public BasicGet(String queueName, Boolean noAck) {
            QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            NoAck     = noAck;
        }
    }

    public sealed class BasicGetEmpty : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x48);
    }

    public sealed class BasicGetOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x47);

        public UInt64 DeliveryTag { get; }
        public Boolean Redelivered { get; }
        public String ExchangeName { get; }
        public String RoutingKey { get; }
        public UInt32 MessageCount { get; }

        public BasicGetOk(UInt64 deliveryTag, Boolean redelivered, String exchangeName, String routingKey, UInt32 messageCount) {
            DeliveryTag  = deliveryTag;
            Redelivered  = redelivered;
            ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            RoutingKey   = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
            MessageCount = messageCount;
        }
    }
}
