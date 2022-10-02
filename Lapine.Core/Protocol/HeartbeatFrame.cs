namespace Lapine.Protocol;

using System.Buffers;

record HeartbeatFrame(UInt16 Channel) : Frame(FrameType.Heartbeat, Channel) {
    static public Boolean Deserialize(UInt16 channel, ReadOnlySpan<Byte> buffer, out Frame? result) {
        result = new HeartbeatFrame(channel);
        return true;
    }

    public override IBufferWriter<Byte> Serialize(IBufferWriter<Byte> buffer) =>
        buffer.WriteUInt8((Byte)Type)
            .WriteUInt16BE(Channel)
            .WriteUInt32BE(0)
            .WriteUInt8(FrameTerminator);
}
