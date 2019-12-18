namespace Lapine.Protocol.Commands {
    using System;

    public sealed class TransactionRollback : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x1E);
    }

    public sealed class TransactionRollbackOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x1F);
    }
}
