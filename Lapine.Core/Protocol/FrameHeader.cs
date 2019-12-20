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

        public static Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out FrameHeader result, out ReadOnlySpan<Byte> remaining) {
            if (buffer.ReadUInt8(out var type, out remaining) &&
                remaining.ReadUInt16BE(out var channel, out remaining) &&
                remaining.ReadUInt32BE(out var size, out remaining))
            {
                result = new FrameHeader((FrameType)type, channel, size);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
