namespace Lapine.Protocol {
    using System;
    using System.Buffers;

    public readonly struct FrameHeader : ISerializable {
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

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt8((Byte)_type)
                .WriteUInt16BE(_channel)
                .WriteUInt32BE(_size);

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
