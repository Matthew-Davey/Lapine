namespace Lapine.Protocol.Commands {
    using System;

    public sealed class BasicDeliver : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x3C);

        public String ConsumerTag { get; }
        public UInt64 DeliveryTag { get; }
        public Boolean Redelivered { get; }
        public String ExchangeName { get; }

        public BasicDeliver(String consumerTag, UInt64 deliveryTag, Boolean redelivered, String exchangeName) {
            ConsumerTag  = consumerTag ?? throw new ArgumentNullException(nameof(consumerTag));
            DeliveryTag  = deliveryTag;
            Redelivered  = redelivered;
            ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
        }
    }
}