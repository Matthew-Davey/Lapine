namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct QueuePurge(String QueueName, Boolean NoWait) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x1E);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(QueueName)
            .WriteBoolean(NoWait);

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueuePurge? result) {
        if (buffer.ReadUInt16BE(out var _) &&
            buffer.ReadShortString(out var queueName) &&
            buffer.ReadBoolean(out var noWait))
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

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueuePurgeOk? result) {
        if (buffer.ReadUInt32BE(out var messageCount)) {
            result = new QueuePurgeOk(messageCount);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
