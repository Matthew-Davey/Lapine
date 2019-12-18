namespace Lapine.Protocol.Commands {
    using System;

    public sealed class ChannelClose : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x28);

        public UInt16 ReplyCode { get; }
        public String ReplyText { get; }

        public ChannelClose(UInt16 replyCode, String replyText) {
            ReplyCode = replyCode;
            ReplyText = replyText ?? throw new ArgumentNullException(nameof(replyText));
        }
    }

    public sealed class ChannelCloseOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x29);
    }
}
