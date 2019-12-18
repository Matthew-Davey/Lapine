namespace Lapine.Protocol.Commands {
    using System;
    using System.Collections.Generic;

    public sealed class QueueBind : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x14);

        public String QueueName { get; }
        public String ExchangeName { get; }
        public String RoutingKey { get; }
        public Boolean NoWait { get; }
        public IDictionary<String, Object> Arguments { get; }

        public QueueBind(String queueName, String exchangeName, String routingKey, Boolean noWait, IDictionary<String, Object> arguments) {
            QueueName    = queueName ?? throw new ArgumentNullException(nameof(queueName));
            ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            RoutingKey   = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
            NoWait       = noWait;
            Arguments    = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }
    }

    public sealed class QueueBindOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x15);
    }
}
