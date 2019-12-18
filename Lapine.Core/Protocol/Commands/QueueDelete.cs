namespace Lapine.Protocol.Commands {
    using System;

    public sealed class QueueDelete : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x28);

        public String QueueName { get; }
        public Boolean IfUnused { get; }
        public Boolean IfEmpty { get; }
        public Boolean NoWait { get; }

        public QueueDelete(String queueName, Boolean ifInused, Boolean ifEmpty, Boolean noWait) {
            QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            IfUnused  = IfUnused;
            IfEmpty   = ifEmpty;
            NoWait    = noWait;
        }
    }

    public sealed class QueueDeleteOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x29);

        public UInt32 MessageCount { get; }

        public QueueDeleteOk(UInt32 messageCount) =>
            MessageCount = messageCount;
    }
}
