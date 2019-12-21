namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Collections.Generic;

    public sealed class ConnectionStart : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x0A);

        public (Byte Major, Byte Minor) Version { get; }
        public String ServerProperties { get; }
        public IList<String> Mechanisms { get; }
        public IList<String> Locales { get; }

        public ConnectionStart((Byte Major, Byte Minor) version, String serverProperties, IList<String> mechanisms, IList<String> locales) {
            Version          = version;
            ServerProperties = serverProperties ?? throw new ArgumentNullException(nameof(serverProperties)); // TODO: Decode property table...
            Mechanisms       = mechanisms ?? throw new ArgumentNullException(nameof(mechanisms));
            Locales          = locales ?? throw new ArgumentNullException(nameof(locales));
        }

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ConnectionStart result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt8(out var major, out surplus) &&
                surplus.ReadUInt8(out var minor, out surplus) &&
                surplus.ReadLongString(out var serverProperties, out surplus) &&
                surplus.ReadLongString(out var mechanisms, out surplus) &&
                surplus.ReadLongString(out var locales, out surplus))
            {
                result = new ConnectionStart((major, minor), serverProperties, mechanisms.Split(' '), locales.Split(' '));
                return true;
            }
            else {
                result = default;
                return false;
            }
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt8(Version.Major)
                .WriteUInt8(Version.Minor)
                .WriteLongString(ServerProperties)
                .WriteLongString(String.Join(' ', Mechanisms))
                .WriteLongString(String.Join(' ', Locales));
    }

    public sealed class ConnectionStartOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x0B);

        public IDictionary<String, Object> PeerProperties { get; }
        public String Mechanism { get; }
        public String Response { get; }
        public String Locale { get; }

        public ConnectionStartOk(IDictionary<String, Object> peerProperties, String mechanism, String response, String locale) {
            PeerProperties = peerProperties ?? throw new ArgumentNullException(nameof(peerProperties));
            Mechanism      = mechanism ?? throw new ArgumentNullException(nameof(mechanism));
            Response       = response ?? throw new ArgumentNullException(nameof(response));
            Locale         = locale ?? throw new ArgumentNullException(nameof(locale));
        }
    }
}
