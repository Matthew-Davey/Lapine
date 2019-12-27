namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Collections.Generic;

    public sealed class QueueDeclare : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x0A);

        public String QueueName { get; }
        public Boolean Passive { get; }
        public Boolean Durable { get; }
        public Boolean Exclusive { get; }
        public Boolean AutoDelete { get; }
        public Boolean NoWait { get; }
        public IReadOnlyDictionary<String, Object> Arguments { get; }

        public QueueDeclare(String queueName, Boolean passive, Boolean durable, Boolean exclusive, Boolean autoDelete, Boolean noWait, IReadOnlyDictionary<String, Object> arguments) {
            QueueName  = queueName ?? throw new ArgumentNullException(nameof(queueName));
            Passive    = passive;
            Durable    = durable;
            Exclusive  = exclusive;
            AutoDelete = autoDelete;
            NoWait     = noWait;
            Arguments  = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(QueueName)
                .WriteBits(Passive, Durable, Exclusive, AutoDelete, NoWait)
                .WriteFieldTable(Arguments);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out QueueDeclare result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var queueName, out surplus) &&
                surplus.ReadBits(out var bits, out surplus) &&
                surplus.ReadFieldTable(out var arguments, out surplus))
            {
                result = new QueueDeclare(queueName, bits[0], bits[1], bits[2], bits[3], bits[4], arguments);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    public sealed class QueueDeclareOk : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x0B);

        public String QueueName { get; }
        public UInt32 MessageCount { get; }
        public UInt32 ConsumerCount { get; }

        public QueueDeclareOk(String queueName, UInt32 messageCount, UInt32 consumerCount) {
            QueueName     = queueName ?? throw new ArgumentNullException(nameof(queueName));
            MessageCount  = messageCount;
            ConsumerCount = consumerCount;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(QueueName)
                .WriteUInt32BE(MessageCount)
                .WriteUInt32BE(ConsumerCount);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out QueueDeclareOk result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var queueName, out surplus) &&
                surplus.ReadUInt32BE(out var messageCount, out surplus) &&
                surplus.ReadUInt32BE(out var consumerCount, out surplus))
            {
                result = new QueueDeclareOk(queueName, messageCount, consumerCount);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
