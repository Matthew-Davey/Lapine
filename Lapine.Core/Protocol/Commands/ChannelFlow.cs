namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Diagnostics.CodeAnalysis;

    sealed class ChannelFlow : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x14);

        public Boolean Active { get; }

        public ChannelFlow(Boolean active) =>
            Active = active;

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteBoolean(Active);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ChannelFlow? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadBoolean(out var active, out surplus)) {
                result = new ChannelFlow(active);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    sealed class ChannelFlowOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x15);

        public Boolean Active { get; }

        public ChannelFlowOk(Boolean active) =>
            Active = active;

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteBoolean(Active);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ChannelFlowOk? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadBoolean(out var active, out surplus)) {
                result = new ChannelFlowOk(active);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
