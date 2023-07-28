namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct BasicDeliver(String ConsumerTag, UInt64 DeliveryTag, Boolean Redelivered, String ExchangeName) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x3C);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteShortString(ConsumerTag)
            .WriteUInt64BE(DeliveryTag)
            .WriteBoolean(Redelivered)
            .WriteShortString(ExchangeName);

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicDeliver? result) {
        if (buffer.ReadShortString(out var consumerTag) &&
            buffer.ReadUInt64BE(out var deliveryTag) &&
            buffer.ReadBoolean(out var redelivered) &&
            buffer.ReadShortString(out var exchangeName))
        {
            result = new BasicDeliver(consumerTag, deliveryTag, redelivered, exchangeName);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
