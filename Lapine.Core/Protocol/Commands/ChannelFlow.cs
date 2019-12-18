namespace Lapine.Protocol.Commands {
    using System;

    public sealed class ChannelFlow : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x14);

        public Boolean Active { get; }

        public ChannelFlow(Boolean active) =>
            Active = active;
    }

    public sealed class ChannelFlowOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x15);

        public Boolean Active { get; }

        public ChannelFlowOk(Boolean active) =>
            Active = active;
    }
}
