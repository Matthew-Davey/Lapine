namespace Lapine.Protocol;

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
}
