namespace Lapine.Protocol.Commands {
    using System;

    public sealed class BasicRecoverAsync : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x64);

        public Boolean ReQueue { get; }

        public BasicRecoverAsync(Boolean requeue) =>
            ReQueue = requeue;
    }

    public sealed class BasicRecover : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x6E);

        public Boolean ReQueue { get; }

        public BasicRecover(Boolean requeue) =>
            ReQueue = requeue;
    }

    public sealed class BasicRecoverOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x6F);
    }
}
