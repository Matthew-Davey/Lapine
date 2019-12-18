namespace Lapine.Protocol.Commands {
    using System;

    public sealed class QueuePurge : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x1E);

        public String QueueName { get; }
        public Boolean NoWait { get; }

        public QueuePurge(String queueName, Boolean noWait) {
            QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            NoWait    = noWait;
        }
    }

    public sealed class QueuePurgeOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x1F);

        public UInt32 MessageCount { get; }

        public QueuePurgeOk(UInt32 messageCount) =>
            MessageCount = messageCount;
    }
}
