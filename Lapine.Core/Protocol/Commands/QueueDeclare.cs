namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    record struct QueueDeclare(String QueueName, Boolean Passive, Boolean Durable, Boolean Exclusive, Boolean AutoDelete, Boolean NoWait, IReadOnlyDictionary<String, Object> Arguments) : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x0A);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt16BE(0) // reserved-1 'ticket'
                .WriteShortString(QueueName)
                .WriteBits(Passive, Durable, Exclusive, AutoDelete, NoWait)
                .WriteFieldTable(Arguments);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueDeclare? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt16BE(out var _, out surplus) &&
                surplus.ReadShortString(out var queueName, out surplus) &&
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

    record struct QueueDeclareOk(String QueueName, UInt32 MessageCount, UInt32 ConsumerCount) : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x0B);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(QueueName)
                .WriteUInt32BE(MessageCount)
                .WriteUInt32BE(ConsumerCount);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueDeclareOk? result, out ReadOnlySpan<Byte> surplus) {
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
