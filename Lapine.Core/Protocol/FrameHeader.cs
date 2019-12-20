namespace Lapine.Protocol {
    using System;
    using System.Buffers;

    using static System.Buffers.Binary.BinaryPrimitives;

    public readonly struct FrameHeader {
        readonly FrameType _type;
        readonly UInt16 _channel;
        readonly UInt32 _size;

        public FrameHeader(in FrameType type, in UInt16 channel, in UInt32 size) {
            _type    = type;
            _channel = channel;
            _size    = size;
        }

        public FrameType Type => _type;
        public UInt16 Channel => _channel;
        public UInt32 Size => _size;

        public void Serialize(IBufferWriter<Byte> writer) {
            var buffer = writer.GetSpan(sizeHint: 7);

            buffer[0] = (Byte)_type;

            WriteUInt16BigEndian(buffer.Slice(1, 2), _channel);
            WriteUInt32BigEndian(buffer.Slice(3, 4), _size);

            writer.Advance(7);
        }

        public static void Deserialize(in ReadOnlySpan<Byte> buffer, out FrameHeader result) {
            var type    = (FrameType)buffer[0];
            var channel = ReadUInt16BigEndian(buffer.Slice(1, 2));
            var size    = ReadUInt32BigEndian(buffer.Slice(3, 4));

            result = new FrameHeader(type, channel, size);
        }
    }
}
