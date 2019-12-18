namespace Lapine.Protocol.Commands {
    using System;
    using System.Collections.Generic;

    public sealed class QueueUnbind : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x32);

        public String QueueName { get; }
        public String ExchangeName { get; }
        public String RoutingKey { get; }
        public IDictionary<String, Object> Arguments { get; }

        public QueueUnbind(String queueName, String exchangeName, String routingKey, IDictionary<String, Object> arguments) {
            QueueName    = queueName ?? throw new ArgumentNullException(nameof(queueName));
            ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            RoutingKey   = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
            Arguments    = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }
    }

    public sealed class QueueUnbindOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x33);
    }
}
