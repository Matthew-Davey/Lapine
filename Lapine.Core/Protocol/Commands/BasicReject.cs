namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct BasicReject(UInt64 DeliveryTag, Boolean ReQueue) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x5A);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt64BE(DeliveryTag)
            .WriteBoolean(ReQueue);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out BasicReject? result) {
        if (BufferExtensions.ReadUInt64BE(ref buffer, out var deliveryTag) &&
            BufferExtensions.ReadBoolean(ref buffer, out var requeue))
        {
            result = new BasicReject(deliveryTag, requeue);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
