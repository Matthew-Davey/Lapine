namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    public sealed class ExchangeDelete : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x14);

        public String ExchangeName { get; }
        public Boolean IfUnused { get; }
        public Boolean NoWait { get; }

        public ExchangeDelete(String exchangeName, Boolean ifUnused, Boolean noWait) {
            ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            IfUnused     = ifUnused;
            NoWait       = noWait;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(ExchangeName)
                .WriteBits(IfUnused, NoWait);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ExchangeDelete result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var exchangeName, out surplus) &&
                surplus.ReadBits(out var bits, out surplus))
            {
                result = new ExchangeDelete(exchangeName, bits[0], bits[1]);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    public sealed class ExchangeDeleteOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x15);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ExchangeDeleteOk result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result = new ExchangeDeleteOk();
            return true;
        }
    }
}
