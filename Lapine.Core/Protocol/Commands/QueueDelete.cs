namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct QueueDelete(String QueueName, Boolean IfUnused, Boolean IfEmpty, Boolean NoWait) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x28);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(QueueName)
            .WriteBits(IfUnused, IfEmpty, NoWait);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out QueueDelete? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out var _) &&
            BufferExtensions.ReadShortString(ref buffer, out var queueName) &&
            BufferExtensions.ReadBits(ref buffer, out var bits))
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

readonly record struct QueueDeleteOk(UInt32 MessageCount) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x29);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt32BE(MessageCount);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out QueueDeleteOk? result) {
        if (BufferExtensions.ReadUInt32BE(ref buffer, out var messageCount)) {
            result = new QueueDeleteOk(messageCount);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
