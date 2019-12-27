namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    public sealed class BasicCancel : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x1E);

        public String ConsumerTag { get; }
        public Boolean NoWait { get; }

        public BasicCancel(String consumerTag, Boolean noWait) {
            ConsumerTag = consumerTag ?? throw new ArgumentNullException(nameof(consumerTag));
            NoWait      = noWait;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(ConsumerTag)
                .WriteBoolean(NoWait);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out BasicCancel result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var consumerTag, out surplus) &&
                surplus.ReadBoolean(out var noWait, out surplus))
            {
                result = new BasicCancel(consumerTag, noWait);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    public sealed class BasicCancelOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x1F);

        public String ConsumerTag { get; }

        public BasicCancelOk(String consumerTag) =>
            ConsumerTag = consumerTag ?? throw new ArgumentNullException(nameof(consumerTag));

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(ConsumerTag);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out BasicCancelOk result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var consumerTag, out surplus)) {
                result = new BasicCancelOk(consumerTag);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
