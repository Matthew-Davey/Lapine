namespace Lapine.Protocol;

record ContentBodyFrame(UInt16 Channel, ReadOnlyMemory<Byte> ContentBody) : Frame(FrameType.Body, Channel) {
    static public Boolean Deserialize(UInt16 channel, ReadOnlySpan<Byte> buffer, out Frame? result) {
        result = new ContentBodyFrame(channel, buffer.ToArray());
        return true;
    }
}
