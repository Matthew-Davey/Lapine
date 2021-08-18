namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    record struct BasicConsume(String QueueName, String ConsumerTag, Boolean NoLocal, Boolean NoAck, Boolean Exclusive, Boolean NoWait, IReadOnlyDictionary<String, Object> Arguments) : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x14);

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

    record struct BasicConsumeOk(String ConsumerTag) : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x15);

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
