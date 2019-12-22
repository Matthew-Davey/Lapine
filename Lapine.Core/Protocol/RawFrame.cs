namespace Lapine.Protocol {
    using System;
    using System.Buffers;

    public readonly struct RawFrame : ISerializable {
        public const Byte FrameTerminator = 0xCE;

        readonly FrameType _type;
        readonly UInt16 _channel;
        readonly ReadOnlyMemory<Byte> _payload;

        public RawFrame(in FrameType type, in UInt16 channel, in ReadOnlyMemory<Byte> payload) {
            if (!Enum.IsDefined(typeof(FrameType), type))
                throw new ProtocolErrorException();

            _type    = type;
            _channel = channel;
            _payload = payload;
        }

        public FrameType Type => _type;
        public UInt16 Channel => _channel;
        public UInt32 Size => (UInt32)_payload.Length;
        public ReadOnlyMemory<Byte> Payload => _payload;

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt8((Byte)Type)
                .WriteUInt16BE(Channel)
                .WriteUInt32BE(Size)
                .WriteBytes(Payload.Span)
                .WriteUInt8(FrameTerminator);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out RawFrame result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt8(out var type, out surplus) &&
                surplus.ReadUInt16BE(out var channel, out surplus) &&
                surplus.ReadUInt32BE(out var size, out surplus) &&
                surplus.ReadBytes(size, out var payload, out surplus) &&
                surplus.ReadUInt8(out var terminator, out surplus))
            {
                if (terminator != FrameTerminator)
                    throw new FramingErrorException();

                result = new RawFrame((FrameType)type, channel, payload.ToArray());
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}