namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Collections.Generic;

    sealed class QueueUnbind : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x32);

        public String QueueName { get; }
        public String ExchangeName { get; }
        public String RoutingKey { get; }
        public IReadOnlyDictionary<String, Object> Arguments { get; }

        public QueueUnbind(String queueName, String exchangeName, String routingKey, IReadOnlyDictionary<String, Object> arguments) {
            QueueName    = queueName ?? throw new ArgumentNullException(nameof(queueName));
            ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            RoutingKey   = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
            Arguments    = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(QueueName)
                .WriteShortString(ExchangeName)
                .WriteShortString(RoutingKey)
                .WriteFieldTable(Arguments);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out QueueUnbind result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var queueName, out surplus) &&
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

    sealed class QueueUnbindOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x33);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out QueueUnbindOk result, out ReadOnlySpan<Byte> surplus) {
            result = new QueueUnbindOk();
            surplus = buffer;
            return true;
        }
    }
}
