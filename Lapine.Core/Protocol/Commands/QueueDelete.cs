namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Diagnostics.CodeAnalysis;

    sealed class QueueDelete : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x28);

        public String QueueName { get; }
        public Boolean IfUnused { get; }
        public Boolean IfEmpty { get; }
        public Boolean NoWait { get; }

        public QueueDelete(String queueName, Boolean ifUnused, Boolean ifEmpty, Boolean noWait) {
            QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            IfUnused  = ifUnused;
            IfEmpty   = ifEmpty;
            NoWait    = noWait;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt16BE(0) // reserved-1
                .WriteShortString(QueueName)
                .WriteBits(IfUnused, IfEmpty, NoWait);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueDelete? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt16BE(out var _, out surplus) &&
                surplus.ReadShortString(out var queueName, out surplus) &&
                surplus.ReadBits(out var bits, out surplus))
            {
                result = new QueueDelete(queueName, bits[0], bits[1], bits[2]);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    sealed class QueueDeleteOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x29);

        public UInt32 MessageCount { get; }

        public QueueDeleteOk(UInt32 messageCount) =>
            MessageCount = messageCount;

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt32BE(MessageCount);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out QueueDeleteOk? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt32BE(out var messageCount, out surplus)) {
                result = new QueueDeleteOk(messageCount);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
