namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct ExchangeDeclare(String ExchangeName, String ExchangeType, Boolean Passive, Boolean Durable, Boolean AutoDelete, Boolean Internal, Boolean NoWait, IReadOnlyDictionary<String, Object> Arguments) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x0A);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer
            .WriteInt16BE(0) // reserved-1
            .WriteShortString(ExchangeName)
            .WriteShortString(ExchangeType)
            .WriteBits(Passive, Durable, AutoDelete, Internal, NoWait)
            .WriteFieldTable(Arguments);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ExchangeDeclare? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out var _) &&
            BufferExtensions.ReadShortString(ref buffer, out var exchangeName) &&
            BufferExtensions.ReadShortString(ref buffer, out var exchangeType) &&
            BufferExtensions.ReadBits(ref buffer, out var bits) &&
            BufferExtensions.ReadFieldTable(ref buffer, out var arguments))
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

readonly record struct ExchangeDeclareOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x0B);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ExchangeDeclareOk? result) {
        result = new ExchangeDeclareOk();
        return true;
    }
}
