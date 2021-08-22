namespace Lapine.Protocol.Commands;

using System;
using System.Buffers;
using System.Collections.Generic;
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

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueBind? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt16BE(out var _, out surplus) &&
            surplus.ReadShortString(out var queueName, out surplus) &&
            surplus.ReadShortString(out var exchangeName, out surplus) &&
            surplus.ReadShortString(out var routingKey, out surplus) &&
            surplus.ReadBoolean(out var noWait, out surplus) &&
            surplus.ReadFieldTable(out var arguments, out surplus))
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

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueBindOk? result, out ReadOnlySpan<Byte> surplus) {
        result  = new QueueBindOk();
        surplus = buffer;
        return true;
    }
}
