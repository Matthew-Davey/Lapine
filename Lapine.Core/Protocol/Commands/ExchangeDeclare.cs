namespace Lapine.Protocol.Commands;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

record struct ExchangeDeclare(String ExchangeName, String ExchangeType, Boolean Passive, Boolean Durable, Boolean AutoDelete, Boolean Internal, Boolean NoWait, IReadOnlyDictionary<String, Object> Arguments) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x0A);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer
            .WriteInt16BE(0) // reserved-1
            .WriteShortString(ExchangeName)
            .WriteShortString(ExchangeType)
            .WriteBits(Passive, Durable, AutoDelete, Internal, NoWait)
            .WriteFieldTable(Arguments);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ExchangeDeclare? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt16BE(out var _, out surplus) &&
            surplus.ReadShortString(out var exchangeName, out surplus) &&
            surplus.ReadShortString(out var exchangeType, out surplus) &&
            surplus.ReadBits(out var bits, out surplus) &&
            surplus.ReadFieldTable(out var arguments, out surplus))
        {
            result = new ExchangeDeclare(exchangeName, exchangeType, bits[0], bits[1], bits[2], bits[3], bits[4], arguments);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

record struct ExchangeDeclareOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x0B);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer;

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ExchangeDeclareOk? result, out ReadOnlySpan<Byte> surplus) {
        surplus = buffer;
        result = new ExchangeDeclareOk();
        return true;
    }
}
