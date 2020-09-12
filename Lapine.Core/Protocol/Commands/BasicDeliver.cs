namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    sealed class BasicDeliver : ICommand {
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

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(ConsumerTag)
                .WriteUInt64BE(DeliveryTag)
                .WriteBoolean(Redelivered)
                .WriteShortString(ExchangeName);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out BasicDeliver result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var consumerTag, out surplus) &&
                surplus.ReadUInt64BE(out var deliveryTag, out surplus) &&
                surplus.ReadBoolean(out var redelivered, out surplus) &&
                surplus.ReadShortString(out var exchangeName, out surplus))
            {
                result = new BasicDeliver(consumerTag, deliveryTag, redelivered, exchangeName);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
