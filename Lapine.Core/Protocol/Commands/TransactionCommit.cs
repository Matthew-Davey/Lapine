namespace Lapine.Protocol.Commands;

using System.Buffers;

readonly record struct TransactionCommit : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x14);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, out TransactionCommit result) {
        result = new TransactionCommit();
        return true;
    }
}

readonly record struct TransactionCommitOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x15);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, out TransactionCommitOk result) {
        result = new TransactionCommitOk();
        return true;
    }
}
