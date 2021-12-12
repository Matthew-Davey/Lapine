namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct QueueUnbind(String QueueName, String ExchangeName, String RoutingKey, IReadOnlyDictionary<String, Object> Arguments) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x32);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(QueueName)
            .WriteShortString(ExchangeName)
            .WriteShortString(RoutingKey)
            .WriteFieldTable(Arguments);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueUnbind? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt16BE(out var _, out surplus) &&
            surplus.ReadShortString(out var queueName, out surplus) &&
            surplus.ReadShortString(out var exchangeName, out surplus) &&
            surplus.ReadShortString(out var routingKey, out surplus) &&
            surplus.ReadFieldTable(out var arguments, out surplus))
        {
            result = new QueueUnbind(queueName, exchangeName, routingKey, arguments);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

record struct QueueUnbindOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x33);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueUnbindOk? result, out ReadOnlySpan<Byte> surplus) {
        result = new QueueUnbindOk();
        surplus = buffer;
        return true;
    }
}
