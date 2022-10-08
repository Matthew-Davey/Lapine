namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct BasicPublish(String ExchangeName, String RoutingKey, Boolean Mandatory, Boolean Immediate): ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x28);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(ExchangeName)
            .WriteShortString(RoutingKey)
            .WriteBits(Mandatory, Immediate);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out BasicPublish? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out _) &&
            BufferExtensions.ReadShortString(ref buffer, out var exchangeName) &&
            BufferExtensions.ReadShortString(ref buffer, out var routingKey) &&
            BufferExtensions.ReadBits(ref buffer, out var bits))
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
