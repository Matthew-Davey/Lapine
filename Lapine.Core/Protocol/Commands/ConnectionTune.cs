namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct ConnectionTune(UInt16 ChannelMax, UInt32 FrameMax, UInt16 Heartbeat) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x1E);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(ChannelMax)
            .WriteUInt32BE(FrameMax)
            .WriteUInt16BE(Heartbeat);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ConnectionTune? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out var channelMax) &&
            BufferExtensions.ReadUInt32BE(ref buffer, out var frameMax) &&
            BufferExtensions.ReadUInt16BE(ref buffer, out var heartbeat))
        {
            result = new ConnectionTune(channelMax, frameMax, heartbeat);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

readonly record struct ConnectionTuneOk(UInt16 ChannelMax, UInt32 FrameMax, UInt16 Heartbeat) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x1F);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(ChannelMax)
            .WriteUInt32BE(FrameMax)
            .WriteUInt16BE(Heartbeat);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ConnectionTuneOk? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out var channelMax) &&
            BufferExtensions.ReadUInt32BE(ref buffer, out var frameMax) &&
            BufferExtensions.ReadUInt16BE(ref buffer, out var heartbeat))
        {
            result = new ConnectionTuneOk(channelMax, frameMax, heartbeat);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
