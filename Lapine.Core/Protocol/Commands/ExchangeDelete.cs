namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct ExchangeDelete(String ExchangeName, Boolean IfUnused, Boolean NoWait) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x14);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(ExchangeName)
            .WriteBits(IfUnused, NoWait);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ExchangeDelete? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out var _) &&
            BufferExtensions.ReadShortString(ref buffer, out var exchangeName) &&
            BufferExtensions.ReadBits(ref buffer, out var bits))
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

readonly record struct ExchangeDeleteOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x15);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ExchangeDeleteOk? result) {
        result = new ExchangeDeleteOk();
        return true;
    }
}
