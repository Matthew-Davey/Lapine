namespace Lapine.Protocol.Commands {
    using System;
    using System.Collections.Generic;

    public sealed class BasicConsume : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x14);

        public String QueueName { get; }
        public String ConsumerTag { get; }
        public Boolean NoLocal { get; }
        public Boolean NoAck { get; }
        public Boolean Exclusive { get; }
        public Boolean NoWait { get; }
        public IDictionary<String, Object> Arguments { get; }

        public BasicConsume(String queueName, String consumerTag, Boolean noLocal, Boolean noAck, Boolean exclusive, Boolean noWait, IDictionary<String, Object> arguments) {
            QueueName   = queueName ?? throw new ArgumentNullException(nameof(queueName));
            ConsumerTag = consumerTag ?? throw new ArgumentNullException(nameof(consumerTag));
            NoLocal     = noLocal;
            NoAck       = noAck;
            Exclusive   = exclusive;
            NoWait      = noWait;
            Arguments   = arguments;
        }
    }

    public sealed class BasicConsumeOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x15);

        public String ConsumerTag { get; }

        public BasicConsumeOk(String consumerTag) =>
            ConsumerTag = consumerTag ?? throw new ArgumentNullException(nameof(consumerTag));
    }
}
