namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct QueueUnbind(String QueueName, String ExchangeName, String RoutingKey, IReadOnlyDictionary<String, Object> Arguments) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x32);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(0) // reserved-1
            .WriteShortString(QueueName)
            .WriteShortString(ExchangeName)
            .WriteShortString(RoutingKey)
            .WriteFieldTable(Arguments);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out QueueUnbind? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out _) &&
            BufferExtensions.ReadShortString(ref buffer, out var queueName) &&
            BufferExtensions.ReadShortString(ref buffer, out var exchangeName) &&
            BufferExtensions.ReadShortString(ref buffer, out var routingKey) &&
            BufferExtensions.ReadFieldTable(ref buffer, out var arguments))
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

readonly record struct QueueUnbindOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x33);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out QueueUnbindOk? result) {
        result = new QueueUnbindOk();
        return true;
    }
}
