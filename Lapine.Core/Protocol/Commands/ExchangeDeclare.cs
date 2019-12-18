namespace Lapine.Protocol.Commands {
    using System;
    using System.Collections.Generic;

    public sealed class ExchangeDeclare : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x0A);

        public String ExchangeName { get; }
        public String ExchangeType { get; }
        public Boolean Passive { get; }
        public Boolean Durable { get; }
        public Boolean NoWait { get; }
        public IDictionary<String, Object> Arguments { get; }

        public ExchangeDeclare(String exchangeName, String exchangeType, Boolean passive, Boolean durable, Boolean noWait, IDictionary<String, Object> arguments) {
            ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            ExchangeType = exchangeType ?? throw new ArgumentNullException(nameof(exchangeType));
            Passive      = passive;
            Durable      = durable;
            NoWait       = noWait;
            Arguments    = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }
    }

    public sealed class ExchangeDeclareOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x0B);
    }
}
