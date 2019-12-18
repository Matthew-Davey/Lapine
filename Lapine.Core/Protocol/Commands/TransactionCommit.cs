namespace Lapine.Protocol.Commands {
    using System;

    public sealed class TransactionCommit : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x14);
    }

    public sealed class TransactionCommitOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x15);
    }
}
