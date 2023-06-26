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

        // TODO: payloadWriter will be collected, but we still have a reference to its memory ref buffer...
        return new RawFrame(FrameType.Method, channel, payload);
    }

    static public RawFrame Wrap(in UInt16 channel, in ContentHeader contentHeader) {
        var payloadWriter = new ArrayBufferWriter<Byte>();
        payloadWriter.WriteSerializable(contentHeader);
        var payload = payloadWriter.WrittenMemory;

        // TODO: payloadWriter will be collected, but we still have a reference to its memory ref buffer...
        return new RawFrame(FrameType.Header, channel, payload);
    }

    static public RawFrame Wrap(in UInt16 channel, in ReadOnlySpan<Byte> content) {
        var payloadWriter = new ArrayBufferWriter<Byte>();
        payloadWriter.WriteBytes(in content);
        var payload = payloadWriter.WrittenMemory;

        // TODO: payloadWriter will be collected, but we still have a reference to its memory ref buffer...
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

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out RawFrame? result) {
        if (buffer.ReadUInt8(out var type) &&
            buffer.ReadUInt16BE(out var channel) &&
            buffer.ReadUInt32BE(out var size) &&
            buffer.ReadBytes(size, out var payload) &&
            buffer.ReadUInt8(out var terminator))
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

    static public ICommand UnwrapMethod(RawFrame frame) {
        if (frame.Type != FrameType.Method)
            throw new ArgumentException("frame type must be Method", nameof(frame));

        var buffer = frame.Payload.Span;

        if (buffer.ReadMethodHeader(out var methodHeader)) {
            return methodHeader switch {
                // Connection class
                (0x0A, 0x0A) => ConnectionStart.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x0A, 0x14) => ConnectionSecure.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x0A, 0x1E) => ConnectionTune.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x0A, 0x29) => ConnectionOpenOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x0A, 0x32) => ConnectionClose.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x0A, 0x33) => ConnectionCloseOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),

                // Channel class
                (0x14, 0x0B) => ChannelOpenOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x14, 0x14) => ChannelFlow.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x14, 0x15) => ChannelFlowOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x14, 0x28) => ChannelClose.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x14, 0x29) => ChannelCloseOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),

                // Exchange class
                (0x28, 0x0B) => ExchangeDeclareOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x28, 0x15) => ExchangeDeleteOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),

                // Queue class
                (0x32, 0x0B) => QueueDeclareOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x32, 0x15) => QueueBindOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x32, 0x33) => QueueUnbindOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x32, 0x1F) => QueuePurgeOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x32, 0x29) => QueueDeleteOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),

                // Basic class
                (0x3C, 0x0B) => BasicQosOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x3C, 0x15) => BasicConsumeOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x3C, 0x1F) => BasicCancelOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x3C, 0x32) => BasicReturn.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x3C, 0x3C) => BasicDeliver.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x3C, 0x47) => BasicGetOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x3C, 0x48) => BasicGetEmpty.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x3C, 0x50) => BasicAck.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x3C, 0x6F) => BasicRecoverOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x3C, 0x78) => BasicNack.Deserialize(ref buffer, out var message) ? message : throw new Exception(),

                // Tx class
                (0x5A, 0x0B) => TransactionSelectOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x5A, 0x15) => TransactionCommitOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x5A, 0x1F) => TransactionRollback.Deserialize(ref buffer, out var message) ? message : throw new Exception(),

                // Confirm class
                (0x55, 0x0A) => ConfirmSelect.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                (0x55, 0x0B) => ConfirmSelectOk.Deserialize(ref buffer, out var message) ? message : throw new Exception(),
                _ => throw new Exception()
            };
        }

        throw new Exception();
    }

    static public ContentHeader UnwrapContentHeader(RawFrame frame) {
        if (frame.Type != FrameType.Header)
            throw new ArgumentException("frame type must be Header", nameof(frame));

        var buffer = frame.Payload.Span;
        if (ContentHeader.Deserialize(ref buffer, out var contentHeader))
            return contentHeader.Value;

        throw new Exception();
    }

    static public ReadOnlyMemory<Byte> UnwrapContentBody(RawFrame frame) {
        if (frame.Type != FrameType.Body)
            throw new ArgumentException("frame type must be Body", nameof(frame));

        return frame.Payload;
    }

    static public Object Unwrap(RawFrame frame) => frame.Type switch {
        FrameType.Body   => UnwrapContentBody(frame),
        FrameType.Header => UnwrapContentHeader(frame),
        FrameType.Method => UnwrapMethod(frame),
        _ => throw new Exception()
    };
}
