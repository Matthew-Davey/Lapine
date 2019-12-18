namespace Lapine.Protocol.Commands {
    using System;

    public sealed class BasicReject : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x5A);

        public UInt64 DeliveryTag { get; }
        public Boolean ReQueue { get; }

        public BasicReject(UInt64 deliveryTag, Boolean requeue) {
            DeliveryTag = deliveryTag;
            ReQueue     = requeue;
        }
    }
}
