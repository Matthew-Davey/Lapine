namespace Lapine.Protocol.Commands {
    using System;

    public sealed class BasicAck : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x50);

        public UInt64 DeliveryTag { get; }
        public Boolean Multiple { get; }

        public BasicAck(UInt64 deliveryTag, Boolean multiple) {
            DeliveryTag = deliveryTag;
            Multiple    = multiple;
        }
    }
}
