namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct BasicPublish(String ExchangeName, String RoutingKey, Boolean Mandatory, Boolean Immediate): ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x28);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(ExchangeName)
            .WriteShortString(RoutingKey)
            .WriteBits(Mandatory, Immediate);

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicPublish? result) {
        if (buffer.ReadUInt16BE(out _) &&
            buffer.ReadShortString(out var exchangeName) &&
            buffer.ReadShortString(out var routingKey) &&
            buffer.ReadBits(out var bits))
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
