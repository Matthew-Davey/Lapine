namespace Lapine.Protocol.Commands;

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct BasicNack(UInt64 DeliveryTag, Boolean Multiple, Boolean ReQueue) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x78);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt64BE(DeliveryTag)
            .WriteBoolean(Multiple)
            .WriteBoolean(ReQueue);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicNack? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt64BE(out var deliveryTag, out surplus) &&
            surplus.ReadBoolean(out var multiple, out surplus) &&
            surplus.ReadBoolean(out var requeue, out surplus))
        {
            result = new BasicNack(deliveryTag, multiple, requeue);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
