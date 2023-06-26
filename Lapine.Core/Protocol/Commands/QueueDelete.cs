namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct QueueDelete(String QueueName, Boolean IfUnused, Boolean IfEmpty, Boolean NoWait) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x28);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(QueueName)
            .WriteBits(IfUnused, IfEmpty, NoWait);

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueDelete? result) {
        if (buffer.ReadUInt16BE(out var _) &&
            buffer.ReadShortString(out var queueName) &&
            buffer.ReadBits(out var bits))
        {
            result = new QueueDelete(queueName, bits[0], bits[1], bits[2]);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

record struct QueueDeleteOk(UInt32 MessageCount) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x29);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt32BE(MessageCount);

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueDeleteOk? result) {
        if (buffer.ReadUInt32BE(out var messageCount)) {
            result = new QueueDeleteOk(messageCount);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
