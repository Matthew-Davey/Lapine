namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct BasicGet(String QueueName, Boolean NoAck) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x46);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(QueueName)
            .WriteBoolean(NoAck);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out BasicGet? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out _) &&
            BufferExtensions.ReadShortString(ref buffer, out var queueName) &&
            BufferExtensions.ReadBoolean(ref buffer, out var noAck))
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

readonly record struct BasicGetOk(UInt64 DeliveryTag, Boolean Redelivered, String ExchangeName, String RoutingKey, UInt32 MessageCount) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x47);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt64BE(DeliveryTag)
            .WriteBoolean(Redelivered)
            .WriteShortString(ExchangeName)
            .WriteShortString(RoutingKey)
            .WriteUInt32BE(MessageCount);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out BasicGetOk? result) {
        if (BufferExtensions.ReadUInt64BE(ref buffer, out var deliveryTag) &&
            BufferExtensions.ReadBoolean(ref buffer, out var redelivered) &&
            BufferExtensions.ReadShortString(ref buffer, out var exchangeName) &&
            BufferExtensions.ReadShortString(ref buffer, out var routingKey) &&
            BufferExtensions.ReadUInt32BE(ref buffer, out var messageCount))
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

readonly record struct BasicGetEmpty : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x48);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out BasicGetEmpty? result) {
        result  = new BasicGetEmpty();
        return true;
    }
}
