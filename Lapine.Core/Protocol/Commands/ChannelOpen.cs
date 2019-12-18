namespace Lapine.Protocol.Commands {
    using System;

    public sealed class ChannelOpen : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x0A);
    }

    public sealed class ChannelOpenOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x0B);
    }
}
