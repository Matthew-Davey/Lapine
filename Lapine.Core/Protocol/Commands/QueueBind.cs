namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct QueueBind(String QueueName, String ExchangeName, String RoutingKey, Boolean NoWait, IReadOnlyDictionary<String, Object> Arguments) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x14);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(QueueName)
            .WriteShortString(ExchangeName)
            .WriteShortString(RoutingKey)
            .WriteBoolean(NoWait)
            .WriteFieldTable(Arguments);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out QueueBind? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out var _) &&
            BufferExtensions.ReadShortString(ref buffer, out var queueName) &&
            BufferExtensions.ReadShortString(ref buffer, out var exchangeName) &&
            BufferExtensions.ReadShortString(ref buffer, out var routingKey) &&
            BufferExtensions.ReadBoolean(ref buffer, out var noWait) &&
            BufferExtensions.ReadFieldTable(ref buffer, out var arguments))
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

readonly record struct QueueBindOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x15);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out QueueBindOk? result) {
        result = new QueueBindOk();
        return true;
    }
}
