namespace Lapine.Protocol;

using System.Buffers;

record ContentBodyFrame(UInt16 Channel, ReadOnlyMemory<Byte> ContentBody) : Frame(FrameType.Body, Channel) {
    static public Boolean Deserialize(UInt16 channel, ReadOnlySpan<Byte> buffer, out Frame? result) {
        result = new ContentBodyFrame(channel, buffer.ToArray());
        return true;
    }

    public override IBufferWriter<Byte> Serialize(IBufferWriter<Byte> buffer) =>
        buffer.WriteUInt8((Byte)Type)
            .WriteUInt16BE(Channel)
            .WriteUInt32BE((UInt32)ContentBody.Length)
            .WriteBytes(ContentBody.Span)
            .WriteUInt8(FrameTerminator);
}
