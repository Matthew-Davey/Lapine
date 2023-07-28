namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct BasicConsume(String QueueName, String ConsumerTag, Boolean NoLocal, Boolean NoAck, Boolean Exclusive, Boolean NoWait, IReadOnlyDictionary<String, Object> Arguments) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x14);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(QueueName)
            .WriteShortString(ConsumerTag)
            .WriteBits(NoLocal, NoAck, Exclusive, NoWait)
            .WriteFieldTable(Arguments);

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicConsume? result) {
        if (buffer.ReadUInt16BE(out var _) &&
            buffer.ReadShortString(out var queueName) &&
            buffer.ReadShortString(out var consumerTag) &&
            buffer.ReadBits(out var bits) &&
            buffer.ReadFieldTable(out var arguments))
        {
            result = new BasicConsume(queueName, consumerTag, bits[0], bits[1], bits[2], bits[3], arguments);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

record struct BasicConsumeOk(String ConsumerTag) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x15);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteShortString(ConsumerTag);

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicConsumeOk? result) {
        if (buffer.ReadShortString(out var consumerTag)) {
            result = new BasicConsumeOk(consumerTag);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
