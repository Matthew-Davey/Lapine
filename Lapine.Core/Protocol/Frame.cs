namespace Lapine.Protocol;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

abstract record Frame(FrameType Type, UInt16 Channel) : ISerializable {
    public const Byte FrameTerminator = 0xCE;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out Frame? result) {
        if (BufferExtensions.ReadUInt8(ref buffer, out var type) &&
            BufferExtensions.ReadUInt16BE(ref buffer, out var channel) &&
            BufferExtensions.ReadUInt32BE(ref buffer, out var length) &&
            BufferExtensions.ReadBytes(ref buffer, length, out var payload) &&
            BufferExtensions.ReadUInt8(ref buffer, out var terminator)) {

            if (terminator != FrameTerminator)
                throw new FramingErrorException();

            return ((FrameType)type) switch {
                FrameType.Method    => MethodFrame.Deserialize(channel, ref payload, out result),
                FrameType.Header    => ContentHeaderFrame.Deserialize(channel, ref payload, out result),
                FrameType.Body      => ContentBodyFrame.Deserialize(channel, ref payload, out result),
                FrameType.Heartbeat => HeartbeatFrame.Deserialize(channel, ref payload, out result),
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
