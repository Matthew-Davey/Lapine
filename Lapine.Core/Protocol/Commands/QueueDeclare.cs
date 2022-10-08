namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct QueueDeclare(String QueueName, Boolean Passive, Boolean Durable, Boolean Exclusive, Boolean AutoDelete, Boolean NoWait, IReadOnlyDictionary<String, Object> Arguments) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x0A);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1 'ticket'
            .WriteShortString(QueueName)
            .WriteBits(Passive, Durable, Exclusive, AutoDelete, NoWait)
            .WriteFieldTable(Arguments);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out QueueDeclare? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out _) &&
            BufferExtensions.ReadShortString(ref buffer, out var queueName) &&
            BufferExtensions.ReadBits(ref buffer, out var bits) &&
            BufferExtensions.ReadFieldTable(ref buffer, out var arguments))
        {
            result = new QueueDeclare(queueName, bits[0], bits[1], bits[2], bits[3], bits[4], arguments);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

readonly record struct QueueDeclareOk(String QueueName, UInt32 MessageCount, UInt32 ConsumerCount) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x0B);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteShortString(QueueName)
            .WriteUInt32BE(MessageCount)
            .WriteUInt32BE(ConsumerCount);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out QueueDeclareOk? result) {
        if (BufferExtensions.ReadShortString(ref buffer, out var queueName) &&
            BufferExtensions.ReadUInt32BE(ref buffer, out var messageCount) &&
            BufferExtensions.ReadUInt32BE(ref buffer, out var consumerCount))
        {
            result = new QueueDeclareOk(queueName, messageCount, consumerCount);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
