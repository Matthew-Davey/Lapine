namespace Lapine.Protocol;

using System.Buffers;

record ContentHeaderFrame(UInt16 Channel, ContentHeader ContentHeader) : Frame(FrameType.Header, Channel) {
    static public Boolean Deserialize(UInt16 channel, ReadOnlySpan<Byte> buffer, out Frame? result) {
        if (Lapine.Protocol.ContentHeader.Deserialize(buffer, out var contentHeader, out var surplus)) {
            result = new ContentHeaderFrame(channel, contentHeader.Value);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }

    public override IBufferWriter<Byte> Serialize(IBufferWriter<Byte> buffer) {
        var payloadWriter = new ArrayBufferWriter<Byte>();
        ContentHeader.Serialize(payloadWriter);

        return buffer.WriteUInt8((Byte)Type)
            .WriteUInt16BE(Channel)
            .WriteUInt32BE((UInt32) payloadWriter.WrittenCount)
            .WriteBytes(payloadWriter.WrittenSpan)
            .WriteUInt8(FrameTerminator);
    }
}
