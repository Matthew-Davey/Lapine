namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Diagnostics.CodeAnalysis;

    record struct BasicGet(String QueueName, Boolean NoAck) : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x46);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt16BE(0) // reserved-1
                .WriteShortString(QueueName)
                .WriteBoolean(NoAck);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicGet? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt16BE(out _, out surplus) &&
                surplus.ReadShortString(out var queueName, out surplus) &&
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

    record struct BasicGetOk(UInt64 DeliveryTag, Boolean Redelivered, String ExchangeName, String RoutingKey, UInt32 MessageCount) : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x47);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt64BE(DeliveryTag)
                .WriteBoolean(Redelivered)
                .WriteShortString(ExchangeName)
                .WriteShortString(RoutingKey)
                .WriteUInt32BE(MessageCount);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicGetOk? result, out ReadOnlySpan<Byte> surplus) {
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

    record struct BasicGetEmpty : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x48);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicGetEmpty? result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result  = new BasicGetEmpty();
            return true;
        }
    }
}
