namespace Lapine.Protocol.Commands {
    using System;
    using System.Collections.Generic;

    public sealed class QueueDeclare : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x0A);

        public String QueueName { get; }
        public Boolean Passive { get; }
        public Boolean Durable { get; }
        public Boolean Exclusive { get; }
        public Boolean AutoDelete { get; }
        public Boolean NoWait { get; }
        public IDictionary<String, Object> Arguments { get; }

        public QueueDeclare(String queueName, Boolean passive, Boolean durable, Boolean exclusive, Boolean autoDelete, Boolean noWait, IDictionary<String, Object> arguments) {
            QueueName  = queueName ?? throw new ArgumentNullException(nameof(queueName));
            Passive    = passive;
            Durable    = durable;
            Exclusive  = exclusive;
            AutoDelete = autoDelete;
            NoWait     = noWait;
            Arguments  = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }
    }

    public sealed class QueueDeclareOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x0B);

        public String QueueName { get; }
        public UInt32 MessageCount { get; }
        public UInt32 ConsumerCount { get; }

        public QueueDeclareOk(String queueName, UInt32 messageCount, UInt32 consumerCount) {
            QueueName     = queueName ?? throw new ArgumentNullException(nameof(queueName));
            MessageCount  = messageCount;
            ConsumerCount = consumerCount;
        }
    }
}
