namespace Lapine.Protocol.Commands {
    using System;

    public sealed class ConnectionClose : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x32);

        public UInt16 ReplyCode { get; }
        public String ReplyText { get; }
        public (Byte ClassId, Byte MethodId) FailingMethod { get; }
        public Byte FailingClassId { get; }
        public Byte FailingMethodId { get; }

        public ConnectionClose(UInt16 replyCode, String replyText, (Byte ClassId, Byte MethodId) failingMethod) {
            ReplyCode     = replyCode;
            ReplyText     = replyText ?? throw new ArgumentNullException(nameof(replyText));
            FailingMethod = failingMethod;
        }
    }

    public sealed class ConnectionCloseOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x33);
    }
}
