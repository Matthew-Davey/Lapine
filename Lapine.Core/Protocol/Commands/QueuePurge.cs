namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct QueuePurge(String QueueName, Boolean NoWait) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x1E);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(QueueName)
            .WriteBoolean(NoWait);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueuePurge? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt16BE(out var _, out surplus) &&
            surplus.ReadShortString(out var queueName, out surplus) &&
            surplus.ReadBoolean(out var noWait, out surplus))
        {
            result = new QueuePurge(queueName, noWait);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

record struct QueuePurgeOk(UInt32 MessageCount) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x1F);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt32BE(MessageCount);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueuePurgeOk? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt32BE(out var messageCount, out surplus)) {
            result = new QueuePurgeOk(messageCount);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
