namespace Lapine.Protocol.Commands;

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct BasicPublish(String ExchangeName, String RoutingKey, Boolean Mandatory, Boolean Immediate): ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x28);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(ExchangeName)
            .WriteShortString(RoutingKey)
            .WriteBits(Mandatory, Immediate);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicPublish? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt16BE(out _, out surplus) &&
            surplus.ReadShortString(out var exchangeName, out surplus) &&
            surplus.ReadShortString(out var routingKey, out surplus) &&
            surplus.ReadBits(out var bits, out surplus))
        {
            result = new BasicPublish(exchangeName, routingKey, bits[0], bits[1]);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
