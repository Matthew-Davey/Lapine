namespace Lapine.Protocol;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Lapine.Protocol.Commands;

readonly record struct RawFrame(FrameType Type, UInt16 Channel, ReadOnlyMemory<Byte> Payload) : ISerializable {
    public const Byte FrameTerminator = 0xCE;

    public UInt32 Size => (UInt32)Payload.Length;

    public UInt32 SerializedSize => 7 + Size + 1; // header + payload + frame-terminator...

    static public RawFrame Wrap(in UInt16 channel, in ICommand command) {
        var payloadWriter = new ArrayBufferWriter<Byte>();
        payloadWriter
            .WriteUInt16BE(command.CommandId.ClassId)
            .WriteUInt16BE(command.CommandId.MethodId)
            .WriteSerializable(command);
        var payload = payloadWriter.WrittenMemory;

        // TODO: payloadWriter will be collected, but we still have a reference to its memory in payload...
        return new RawFrame(FrameType.Method, channel, payload);
    }

    static public RawFrame Wrap(in UInt16 channel, in ContentHeader contentHeader) {
        var payloadWriter = new ArrayBufferWriter<Byte>();
        payloadWriter.WriteSerializable(contentHeader);
        var payload = payloadWriter.WrittenMemory;

        // TODO: payloadWriter will be collected, but we still have a reference to its memory in payload...
        return new RawFrame(FrameType.Header, channel, payload);
    }

    static public RawFrame Wrap(in UInt16 channel, in ReadOnlySpan<Byte> content) {
        var payloadWriter = new ArrayBufferWriter<Byte>();
        payloadWriter.WriteBytes(in content);
        var payload = payloadWriter.WrittenMemory;

        // TODO: payloadWriter will be collected, but we still have a reference to its memory in payload...
        return new RawFrame(FrameType.Body, channel, payload);
    }

    static public RawFrame Heartbeat =>
        new (FrameType.Heartbeat, Channel: 0, Memory<Byte>.Empty);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt8((Byte)Type)
            .WriteUInt16BE(Channel)
            .WriteUInt32BE(Size)
            .WriteBytes(Payload.Span)
            .WriteUInt8(FrameTerminator);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out RawFrame? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt8(out var type, out surplus) &&
            surplus.ReadUInt16BE(out var channel, out surplus) &&
            surplus.ReadUInt32BE(out var size, out surplus) &&
            surplus.ReadBytes(size, out var payload, out surplus) &&
            surplus.ReadUInt8(out var terminator, out surplus))
        {
            if (terminator != FrameTerminator)
                throw new FramingErrorException();

            if (Enum.IsDefined((FrameType)type) == false)
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
