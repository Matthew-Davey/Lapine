namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct QueueBind(String QueueName, String ExchangeName, String RoutingKey, Boolean NoWait, IReadOnlyDictionary<String, Object> Arguments) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x14);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(QueueName)
            .WriteShortString(ExchangeName)
            .WriteShortString(RoutingKey)
            .WriteBoolean(NoWait)
            .WriteFieldTable(Arguments);

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueBind? result) {
        if (buffer.ReadUInt16BE(out var _) &&
            buffer.ReadShortString(out var queueName) &&
            buffer.ReadShortString(out var exchangeName) &&
            buffer.ReadShortString(out var routingKey) &&
            buffer.ReadBoolean(out var noWait) &&
            buffer.ReadFieldTable(out var arguments))
        {
            result = new QueueBind(queueName, exchangeName, routingKey, noWait, arguments);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

record struct QueueBindOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x15);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer;

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueBindOk? result) {
        result  = new QueueBindOk();
        return true;
    }
}
