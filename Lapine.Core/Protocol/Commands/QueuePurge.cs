namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    public sealed class QueuePurge : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x1E);

        public String QueueName { get; }
        public Boolean NoWait { get; }

        public QueuePurge(String queueName, Boolean noWait) {
            QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            NoWait    = noWait;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(QueueName)
                .WriteBoolean(NoWait);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out QueuePurge result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var queueName, out surplus) &&
                surplus.ReadBoolean(out var noWait, out surplus))
            {
                result = new QueuePurge(queueName, noWait);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    public sealed class QueuePurgeOk : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x32, 0x1F);

        public UInt32 MessageCount { get; }

        public QueuePurgeOk(UInt32 messageCount) =>
            MessageCount = messageCount;

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt32BE(MessageCount);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out QueuePurgeOk result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt32BE(out var messageCount, out surplus)) {
                result = new QueuePurgeOk(messageCount);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
