namespace Lapine.Protocol.Commands;

using System.Buffers;

readonly record struct TransactionSelect : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x0A);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, out TransactionSelect result) {
        result = new TransactionSelect();
        return true;
    }
}

readonly record struct TransactionSelectOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x0B);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, out TransactionSelectOk result) {
        result = new TransactionSelectOk();
        return true;
    }
}
