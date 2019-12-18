namespace Lapine.Protocol.Commands {
    using System;

    public sealed class BasicCancel : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x1E);

        public String ConsumerTag { get; }
        public Boolean NoWait { get; }

        public BasicCancel(String consumerTag, Boolean noWait) {
            ConsumerTag = consumerTag ?? throw new ArgumentNullException(nameof(consumerTag));
            NoWait      = noWait;
        }
    }

    public sealed class BasicCancelOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x1F);

        public String ConsumerTag { get; }

        public BasicCancelOk(String consumerTag) =>
            ConsumerTag = consumerTag ?? throw new ArgumentNullException(nameof(consumerTag));
    }
}
