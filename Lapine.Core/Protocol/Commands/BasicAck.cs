namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    public sealed class BasicAck : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x50);

        public UInt64 DeliveryTag { get; }
        public Boolean Multiple { get; }

        public BasicAck(UInt64 deliveryTag, Boolean multiple) {
            DeliveryTag = deliveryTag;
            Multiple    = multiple;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt64BE(DeliveryTag)
                .WriteBoolean(Multiple);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out BasicAck result, out ReadOnlySpan<Byte> surplus) {
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
}
