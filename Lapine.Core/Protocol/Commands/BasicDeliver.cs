namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Diagnostics.CodeAnalysis;

    record struct BasicDeliver(String ConsumerTag, UInt64 DeliveryTag, Boolean Redelivered, String ExchangeName) : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x3C);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(ConsumerTag)
                .WriteUInt64BE(DeliveryTag)
                .WriteBoolean(Redelivered)
                .WriteShortString(ExchangeName);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicDeliver? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var consumerTag, out surplus) &&
                surplus.ReadUInt64BE(out var deliveryTag, out surplus) &&
                surplus.ReadBoolean(out var redelivered, out surplus) &&
                surplus.ReadShortString(out var exchangeName, out surplus))
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
}
