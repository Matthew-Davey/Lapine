namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Diagnostics.CodeAnalysis;

    sealed class ChannelOpen : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x0A);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteShortString(String.Empty); // reserved_1

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ChannelOpen? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var reserved, out surplus)) {
                result = new ChannelOpen();
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    sealed class ChannelOpenOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x0B);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteLongString(String.Empty); // reserved_1

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ChannelOpenOk? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadLongString(out var reserved, out surplus)) {
                result = new ChannelOpenOk();
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
