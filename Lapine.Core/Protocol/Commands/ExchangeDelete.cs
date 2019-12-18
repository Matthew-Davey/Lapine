namespace Lapine.Protocol.Commands {
    using System;

    public sealed class ExchangeDelete : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x14);

        public String ExchangeName { get; }
        public Boolean IfUnused { get; }
        public Boolean NoWait { get; }

        public ExchangeDelete(String exchangeName, Boolean ifUnused, Boolean noWait) {
            ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            IfUnused     = ifUnused;
            NoWait       = noWait;
        }
    }

    public sealed class ExchangeDeleteOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x15);
    }
}
