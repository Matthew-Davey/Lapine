namespace Lapine.Protocol.Commands;

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct BasicAck(UInt64 DeliveryTag, Boolean Multiple) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x50);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt64BE(DeliveryTag)
            .WriteBoolean(Multiple);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicAck? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt64BE(out var deliveryTag, out surplus) &&
            surplus.ReadBoolean(out var multiple, out surplus))
        {
            result = new BasicAck(deliveryTag, multiple);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
