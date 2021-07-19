namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    sealed class BasicConsume : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x14);

        public String QueueName { get; }
        public String ConsumerTag { get; }
        public Boolean NoLocal { get; }
        public Boolean NoAck { get; }
        public Boolean Exclusive { get; }
        public Boolean NoWait { get; }
        public IReadOnlyDictionary<String, Object> Arguments { get; }

        public BasicConsume(String queueName, String consumerTag, Boolean noLocal, Boolean noAck, Boolean exclusive, Boolean noWait, IReadOnlyDictionary<String, Object> arguments) {
            QueueName   = queueName ?? throw new ArgumentNullException(nameof(queueName));
            ConsumerTag = consumerTag ?? throw new ArgumentNullException(nameof(consumerTag));
            NoLocal     = noLocal;
            NoAck       = noAck;
            Exclusive   = exclusive;
            NoWait      = noWait;
            Arguments   = arguments;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt16BE(0) // reserved-1
                .WriteShortString(QueueName)
                .WriteShortString(ConsumerTag)
                .WriteBits(NoLocal, NoAck, Exclusive, NoWait)
                .WriteFieldTable(Arguments);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicConsume? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt16BE(out var _, out surplus) &&
                surplus.ReadShortString(out var queueName, out surplus) &&
                surplus.ReadShortString(out var consumerTag, out surplus) &&
                surplus.ReadBits(out var bits, out surplus) &&
                surplus.ReadFieldTable(out var arguments, out surplus))
            {
                result = new BasicConsume(queueName, consumerTag, bits[0], bits[1], bits[2], bits[3], arguments);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    sealed class BasicConsumeOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x15);

        public String ConsumerTag { get; }

        public BasicConsumeOk(String consumerTag) =>
            ConsumerTag = consumerTag ?? throw new ArgumentNullException(nameof(consumerTag));

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(ConsumerTag);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicConsumeOk? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var consumerTag, out surplus)) {
                result = new BasicConsumeOk(consumerTag);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
