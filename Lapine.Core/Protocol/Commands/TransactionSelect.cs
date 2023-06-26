namespace Lapine.Protocol.Commands;

using System.Buffers;

record struct TransactionSelect : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x0A);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, out TransactionSelect result) {
        result = new TransactionSelect();
        return true;
    }
}

record struct TransactionSelectOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x0B);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, out TransactionSelectOk result) {
        result = new TransactionSelectOk();
        return true;
    }
}
