namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    public sealed class ChannelClose : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x28);

        public UInt16 ReplyCode { get; }
        public String ReplyText { get; }

        public (UInt16 ClassId, UInt16 MethodId) FailingMethod { get; }

        public ChannelClose(UInt16 replyCode, String replyText, (UInt16 ClassId, UInt16 MethodId) failingMethod) {
            ReplyCode     = replyCode;
            ReplyText     = replyText ?? throw new ArgumentNullException(nameof(replyText));
            FailingMethod = failingMethod;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt16BE(ReplyCode)
                .WriteShortString(ReplyText)
                .WriteUInt16BE(FailingMethod.ClassId)
                .WriteUInt16BE(FailingMethod.MethodId);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ChannelClose result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt16BE(out var replyCode, out surplus) &&
                surplus.ReadShortString(out var replyText, out surplus) &&
                surplus.ReadUInt16BE(out var classId, out surplus) &&
                surplus.ReadUInt16BE(out var methodId, out surplus))
            {
                result = new ChannelClose(replyCode, replyText, (classId, methodId));
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    public sealed class ChannelCloseOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x29);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ChannelCloseOk result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result = new ChannelCloseOk();
            return true;
        }
    }
}
