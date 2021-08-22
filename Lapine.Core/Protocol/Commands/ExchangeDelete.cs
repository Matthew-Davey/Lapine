namespace Lapine.Protocol.Commands;

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct ExchangeDelete(String ExchangeName, Boolean IfUnused, Boolean NoWait) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x14);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(ExchangeName)
            .WriteBits(IfUnused, NoWait);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ExchangeDelete? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt16BE(out var _, out surplus) &&
            surplus.ReadShortString(out var exchangeName, out surplus) &&
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

record struct ExchangeDeleteOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x15);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer;

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ExchangeDeleteOk? result, out ReadOnlySpan<Byte> surplus) {
        surplus = buffer;
        result = new ExchangeDeleteOk();
        return true;
    }
}
