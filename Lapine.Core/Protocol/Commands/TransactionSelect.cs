namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    sealed class TransactionSelect : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x0A);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out TransactionSelect result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result = new TransactionSelect();
            return true;
        }
    }

    sealed class TransactionSelectOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x5A, 0x0B);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out TransactionSelectOk result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result = new TransactionSelectOk();
            return true;
        }
    }
}
