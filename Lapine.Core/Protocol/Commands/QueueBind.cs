namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Collections.Generic;

    sealed class QueueBind : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x14);

        public String QueueName { get; }
        public String ExchangeName { get; }
        public String RoutingKey { get; }
        public Boolean NoWait { get; }
        public IReadOnlyDictionary<String, Object> Arguments { get; }

        public QueueBind(String queueName, String exchangeName, String routingKey, Boolean noWait, IReadOnlyDictionary<String, Object> arguments) {
            QueueName    = queueName ?? throw new ArgumentNullException(nameof(queueName));
            ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            RoutingKey   = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
            NoWait       = noWait;
            Arguments    = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(QueueName)
                .WriteShortString(ExchangeName)
                .WriteShortString(RoutingKey)
                .WriteBoolean(NoWait)
                .WriteFieldTable(Arguments);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out QueueBind result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var queueName, out surplus) &&
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

    sealed class QueueBindOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x15);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out QueueBindOk result, out ReadOnlySpan<Byte> surplus) {
            result  = new QueueBindOk();
            surplus = buffer;
            return true;
        }
    }
}
