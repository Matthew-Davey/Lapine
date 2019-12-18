namespace Lapine.Protocol.Commands {
    using System;

    public sealed class ConnectionTune : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x1E);

        public UInt16 ChannelMax { get; }
        public UInt32 FrameMax { get; }
        public UInt16 Heartbeat { get; }

        public ConnectionTune(UInt16 channelMax, UInt32 frameMax, UInt16 heartbeat) {
            ChannelMax = channelMax;
            FrameMax   = frameMax;
            Heartbeat  = heartbeat;
        }
    }

    public sealed class ConnectionTuneOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x1F);

        public UInt16 ChannelMax { get; }
        public UInt32 FrameMax { get; }
        public UInt16 Heartbeat { get; }

        public ConnectionTuneOk(UInt16 channelMax, UInt32 frameMax, UInt16 heartbeat) {
            ChannelMax = channelMax;
            FrameMax   = frameMax;
            Heartbeat  = heartbeat;
        }
    }
}
