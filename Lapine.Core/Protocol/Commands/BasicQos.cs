namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    public sealed class BasicQos : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x0A);

        public UInt32 PrefetchSize { get; }
        public UInt16 PrefetchCount { get; }
        public Boolean Global { get; }

        public BasicQos(UInt32 prefetchSize, UInt16 prefetchCount, Boolean global) {
            PrefetchSize  = prefetchSize;
            PrefetchCount = prefetchCount;
            Global        = global;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt32BE(PrefetchSize)
                .WriteUInt16BE(PrefetchCount)
                .WriteBoolean(Global);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out BasicQos result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt32BE(out var prefetchSize, out surplus) &&
                surplus.ReadUInt16BE(out var prefetchCount, out surplus) &&
                surplus.ReadBoolean(out var global, out surplus))
            {
                result = new BasicQos(prefetchSize, prefetchCount, global);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    public sealed class BasicQosOk : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x0B);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out BasicQosOk result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result  = new BasicQosOk();
            return true;
        }
    }
}
