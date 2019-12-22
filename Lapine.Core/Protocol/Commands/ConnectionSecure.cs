namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;

    public sealed class ConnectionSecure : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x14);

        public String Challenge { get; }

        public ConnectionSecure(String challenge) =>
            Challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteLongString(Challenge);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ConnectionSecure result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadLongString(out var challenge, out surplus)) {
                result = new ConnectionSecure(challenge);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    public sealed class ConnectionSecureOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x15);

        public String Response { get; }

        public ConnectionSecureOk(String response) =>
            Response = response ?? throw new ArgumentNullException(nameof(response));
    }
}
