namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Diagnostics.CodeAnalysis;

    record struct BasicCancel(String ConsumerTag, Boolean NoWait): ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x1E);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(ConsumerTag)
                .WriteBoolean(NoWait);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicCancel? result, out ReadOnlySpan<Byte> surplus) {
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

    record struct BasicCancelOk(String ConsumerTag) : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x1F);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(ConsumerTag);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicCancelOk? result, out ReadOnlySpan<Byte> surplus) {
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
