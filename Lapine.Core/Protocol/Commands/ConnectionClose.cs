namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Diagnostics.CodeAnalysis;

    sealed class ConnectionClose : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x32);

        public UInt16 ReplyCode { get; }
        public String ReplyText { get; }
        public (UInt16 ClassId, UInt16 MethodId) FailingMethod { get; }

        public ConnectionClose(UInt16 replyCode, String replyText, (UInt16 ClassId, UInt16 MethodId) failingMethod) {
            ReplyCode     = replyCode;
            ReplyText     = replyText ?? throw new ArgumentNullException(nameof(replyText));
            FailingMethod = failingMethod;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt16BE(ReplyCode)
                .WriteShortString(ReplyText)
                .WriteUInt16BE(FailingMethod.ClassId)
                .WriteUInt16BE(FailingMethod.MethodId);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConnectionClose? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt16BE(out var replyCode, out surplus) &&
                surplus.ReadShortString(out var replyText, out surplus) &&
                surplus.ReadUInt16BE(out var failingClassId, out surplus) &&
                surplus.ReadUInt16BE(out var failingMethodId, out surplus))
            {
                result = new ConnectionClose(replyCode, replyText, (failingClassId, failingMethodId));
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    sealed class ConnectionCloseOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x33);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConnectionCloseOk? result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result  = new ConnectionCloseOk();
            return true;
        }
    }
}
