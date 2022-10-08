namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct QueuePurge(String QueueName, Boolean NoWait) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x1E);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(QueueName)
            .WriteBoolean(NoWait);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out QueuePurge? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out _) &&
            BufferExtensions.ReadShortString(ref buffer, out var queueName) &&
            BufferExtensions.ReadBoolean(ref buffer, out var noWait))
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

readonly record struct QueuePurgeOk(UInt32 MessageCount) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x1F);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt32BE(MessageCount);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out QueuePurgeOk? result) {
        if (BufferExtensions.ReadUInt32BE(ref buffer, out var messageCount)) {
            result = new QueuePurgeOk(messageCount);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
