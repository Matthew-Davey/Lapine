namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    public sealed class BasicPublish : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x28);

        public String ExchangeName { get; }
        public String RoutingKey { get; }
        public Boolean Mandatory { get; }
        public Boolean Immediate { get; }

        public BasicPublish(String exchangeName, String routingKey, Boolean mandatory, Boolean immediate) {
            ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            RoutingKey   = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
            Mandatory    = mandatory;
            Immediate    = immediate;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(ExchangeName)
                .WriteShortString(RoutingKey)
                .WriteBits(Mandatory, Immediate);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out BasicPublish result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var exchangeName, out surplus) &&
                surplus.ReadShortString(out var routingKey, out surplus) &&
                surplus.ReadBits(out var bits, out surplus))
            {
                result = new BasicPublish(exchangeName, routingKey, bits[0], bits[1]);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
