namespace Lapine.Protocol;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

abstract record Frame(FrameType Type, UInt16 Channel) : ISerializable {
    public const Byte FrameTerminator = 0xCE;

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out Frame? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt8(out var type, out surplus) &&
            surplus.ReadUInt16BE(out var channel, out surplus) &&
            surplus.ReadUInt32BE(out var length, out surplus) &&
            surplus.ReadBytes(length, out var payload, out surplus) &&
            surplus.ReadUInt8(out var terminator, out surplus)) {

            if (terminator != FrameTerminator)
                throw new FramingErrorException();

            return ((FrameType)type) switch {
                FrameType.Method    => MethodFrame.Deserialize(channel, payload, out result),
                FrameType.Header    => ContentHeaderFrame.Deserialize(channel, payload, out result),
                FrameType.Body      => ContentBodyFrame.Deserialize(channel, payload, out result),
                FrameType.Heartbeat => HeartbeatFrame.Deserialize(channel, payload, out result),
                _                   => throw new FramingErrorException($"Unexpected frame type '{type}'")
            };
        }
        else {
            result = default;
            return false;
        }
    }

    public abstract IBufferWriter<Byte> Serialize(IBufferWriter<Byte> buffer);
}
