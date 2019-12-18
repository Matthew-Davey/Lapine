namespace Lapine.Protocol.Commands {
    using System;

    public sealed class TransactionSelect : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x0A);
    }

    public sealed class TransactionSelectOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x0B);
    }
}
