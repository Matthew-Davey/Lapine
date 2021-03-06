namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Diagnostics.CodeAnalysis;

    sealed class BasicReject : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x5A);

        public UInt64 DeliveryTag { get; }
        public Boolean ReQueue { get; }

        public BasicReject(UInt64 deliveryTag, Boolean requeue) {
            DeliveryTag = deliveryTag;
            ReQueue     = requeue;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt64BE(DeliveryTag)
                .WriteBoolean(ReQueue);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicReject? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt64BE(out var deliveryTag, out surplus) &&
                surplus.ReadBoolean(out var requeue, out surplus))
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
}
