namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    public sealed class BasicGet : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x46);

        public String QueueName { get; }
        public Boolean NoAck { get; }

        public BasicGet(String queueName, Boolean noAck) {
            QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            NoAck     = noAck;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(QueueName)
                .WriteBoolean(NoAck);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out BasicGet result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var queueName, out surplus) &&
                surplus.ReadBoolean(out var noAck, out surplus))
            {
                result = new BasicGet(queueName, noAck);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    public sealed class BasicGetEmpty : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x48);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out BasicGetEmpty result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result  = new BasicGetEmpty();
            return true;
        }
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

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt64BE(DeliveryTag)
                .WriteBoolean(Redelivered)
                .WriteShortString(ExchangeName)
                .WriteShortString(RoutingKey)
                .WriteUInt32BE(MessageCount);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out BasicGetOk result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt64BE(out var deliveryTag, out surplus) &&
                surplus.ReadBoolean(out var redelivered, out surplus) &&
                surplus.ReadShortString(out var exchangeName, out surplus) &&
                surplus.ReadShortString(out var routingKey, out surplus) &&
                surplus.ReadUInt32BE(out var messageCount, out surplus))
            {
                result = new BasicGetOk(deliveryTag, redelivered, exchangeName, routingKey, messageCount);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
