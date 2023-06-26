namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct BasicAck(UInt64 DeliveryTag, Boolean Multiple) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x50);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt64BE(DeliveryTag)
            .WriteBoolean(Multiple);

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicAck? result) {
        if (buffer.ReadUInt64BE(out var deliveryTag) &&
            buffer.ReadBoolean(out var multiple))
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
