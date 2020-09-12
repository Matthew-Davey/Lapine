namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    sealed class ChannelOpen : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x0A);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ChannelOpen result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result = new ChannelOpen();
            return true;
        }
    }

    sealed class ChannelOpenOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x14, 0x0B);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ChannelOpenOk result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result = new ChannelOpenOk();
            return true;
        }
    }
}
