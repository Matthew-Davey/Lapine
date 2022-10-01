namespace Lapine.Protocol;

record HeartbeatFrame(UInt16 Channel) : Frame(FrameType.Heartbeat, Channel) {
    static public Boolean Deserialize(UInt16 channel, ReadOnlySpan<Byte> buffer, out Frame? result) {
        result = new HeartbeatFrame(channel);
        return true;
    }
}
