namespace Lapine.Protocol.Commands;

using System.Buffers;

readonly record struct TransactionRollback : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x1E);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, out TransactionRollback result) {
        result = new TransactionRollback();
        return true;
    }
}

readonly record struct TransactionRollbackOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x1F);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, out TransactionRollbackOk result) {
        result = new TransactionRollbackOk();
        return true;
    }
}
