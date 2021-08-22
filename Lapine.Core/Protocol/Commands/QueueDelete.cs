namespace Lapine.Protocol.Commands;

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct QueueDelete(String QueueName, Boolean IfUnused, Boolean IfEmpty, Boolean NoWait) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x28);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(QueueName)
            .WriteBits(IfUnused, IfEmpty, NoWait);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueDelete? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt16BE(out var _, out surplus) &&
            surplus.ReadShortString(out var queueName, out surplus) &&
            surplus.ReadBits(out var bits, out surplus))
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

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueDeleteOk? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt32BE(out var messageCount, out surplus)) {
            result = new QueueDeleteOk(messageCount);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
