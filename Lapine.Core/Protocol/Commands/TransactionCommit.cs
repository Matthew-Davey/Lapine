namespace Lapine.Protocol.Commands;

using System.Buffers;

record struct TransactionCommit : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x14);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out TransactionCommit result, out ReadOnlySpan<Byte> surplus) {
        surplus = buffer;
        result = new TransactionCommit();
        return true;
    }
}

record struct TransactionCommitOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x15);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out TransactionCommitOk result, out ReadOnlySpan<Byte> surplus) {
        surplus = buffer;
        result = new TransactionCommitOk();
        return true;
    }
}
