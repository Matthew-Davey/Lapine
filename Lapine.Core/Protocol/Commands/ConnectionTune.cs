namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Diagnostics.CodeAnalysis;

    sealed class ConnectionTune : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x1E);

        public UInt16 ChannelMax { get; }
        public UInt32 FrameMax { get; }
        public UInt16 Heartbeat { get; }

        public ConnectionTune(in UInt16 channelMax, in UInt32 frameMax, in UInt16 heartbeat) {
            ChannelMax = channelMax;
            FrameMax   = frameMax;
            Heartbeat  = heartbeat;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt16BE(ChannelMax)
                .WriteUInt32BE(FrameMax)
                .WriteUInt16BE(Heartbeat);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConnectionTune? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt16BE(out var channelMax, out surplus) &&
                surplus.ReadUInt32BE(out var frameMax, out surplus) &&
                surplus.ReadUInt16BE(out var heartbeat, out surplus))
            {
                result = new ConnectionTune(in channelMax, in frameMax, in heartbeat);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    sealed class ConnectionTuneOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x1F);

        public UInt16 ChannelMax { get; }
        public UInt32 FrameMax { get; }
        public UInt16 Heartbeat { get; }

        public ConnectionTuneOk(in UInt16 channelMax, in UInt32 frameMax, in UInt16 heartbeat) {
            ChannelMax = channelMax;
            FrameMax   = frameMax;
            Heartbeat  = heartbeat;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt16BE(ChannelMax)
                .WriteUInt32BE(FrameMax)
                .WriteUInt16BE(Heartbeat);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConnectionTuneOk? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt16BE(out var channelMax, out surplus) &&
                surplus.ReadUInt32BE(out var frameMax, out surplus) &&
                surplus.ReadUInt16BE(out var heartbeat, out surplus))
            {
                result = new ConnectionTuneOk(in channelMax, in frameMax, in heartbeat);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
