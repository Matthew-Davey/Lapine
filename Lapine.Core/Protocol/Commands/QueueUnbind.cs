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

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueUnbind? result) {
        if (buffer.ReadUInt16BE(out var _) &&
            buffer.ReadShortString(out var queueName) &&
            buffer.ReadShortString(out var exchangeName) &&
            buffer.ReadShortString(out var routingKey) &&
            buffer.ReadFieldTable(out var arguments))
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

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueUnbindOk? result) {
        result = new QueueUnbindOk();
        return true;
    }
}
