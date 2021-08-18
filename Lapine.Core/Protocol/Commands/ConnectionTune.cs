namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Diagnostics.CodeAnalysis;

    record struct ConnectionTune(UInt16 ChannelMax, UInt32 FrameMax, UInt16 Heartbeat) : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x1E);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt16BE(ChannelMax)
                .WriteUInt32BE(FrameMax)
                .WriteUInt16BE(Heartbeat);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConnectionTune? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt16BE(out var channelMax, out surplus) &&
                surplus.ReadUInt32BE(out var frameMax, out surplus) &&
                surplus.ReadUInt16BE(out var heartbeat, out surplus))
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

    record struct ConnectionTuneOk(UInt16 ChannelMax, UInt32 FrameMax, UInt16 Heartbeat) : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x1F);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt16BE(ChannelMax)
                .WriteUInt32BE(FrameMax)
                .WriteUInt16BE(Heartbeat);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConnectionTuneOk? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt16BE(out var channelMax, out surplus) &&
                surplus.ReadUInt32BE(out var frameMax, out surplus) &&
                surplus.ReadUInt16BE(out var heartbeat, out surplus))
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
}
