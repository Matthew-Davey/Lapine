namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    public sealed class BasicRecover : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x6E);

        public Boolean ReQueue { get; }

        public BasicRecover(Boolean requeue) =>
            ReQueue = requeue;

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteBoolean(ReQueue);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out BasicRecover result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadBoolean(out var requeue, out surplus)) {
                result = new BasicRecover(requeue);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    public sealed class BasicRecoverOk : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x6F);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out BasicRecoverOk result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result  = new BasicRecoverOk();
            return true;
        }
    }
}
