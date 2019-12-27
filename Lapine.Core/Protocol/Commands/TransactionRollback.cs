namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    public sealed class TransactionRollback : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x1E);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out TransactionRollback result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result = new TransactionRollback();
            return true;
        }
    }

    public sealed class TransactionRollbackOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x1F);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out TransactionRollbackOk result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result = new TransactionRollbackOk();
            return true;
        }
    }
}
