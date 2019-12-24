namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Collections.Generic;

    using static System.String;

    public sealed class ConnectionStart : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x0A);

        public (Byte Major, Byte Minor) Version { get; }
        public IReadOnlyDictionary<String, Object> ServerProperties { get; }
        public IList<String> Mechanisms { get; }
        public IList<String> Locales { get; }

        public ConnectionStart((Byte Major, Byte Minor) version, IReadOnlyDictionary<String, Object> serverProperties, IList<String> mechanisms, IList<String> locales) {
            Version          = version;
            ServerProperties = serverProperties ?? throw new ArgumentNullException(nameof(serverProperties));
            Mechanisms       = mechanisms ?? throw new ArgumentNullException(nameof(mechanisms));
            Locales          = locales ?? throw new ArgumentNullException(nameof(locales));
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt8(Version.Major)
                .WriteUInt8(Version.Minor)
                .WriteFieldTable(ServerProperties)
                .WriteLongString(Join(' ', Mechanisms))
                .WriteLongString(Join(' ', Locales));

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ConnectionStart result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt8(out var major, out surplus) &&
                surplus.ReadUInt8(out var minor, out surplus) &&
                surplus.ReadFieldTable(out var serverProperties, out surplus) &&
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
    }

    public sealed class ConnectionStartOk : ICommand, ISerializable {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x0B);

        public IReadOnlyDictionary<String, Object> PeerProperties { get; } // TODO: Decode property table
        public String Mechanism { get; }
        public String Response { get; }
        public String Locale { get; }

        public ConnectionStartOk(IReadOnlyDictionary<String, Object> peerProperties, String mechanism, String response, String locale) {
            PeerProperties = peerProperties ?? throw new ArgumentNullException(nameof(peerProperties));
            Mechanism      = mechanism ?? throw new ArgumentNullException(nameof(mechanism));
            Response       = response ?? throw new ArgumentNullException(nameof(response));
            Locale         = locale ?? throw new ArgumentNullException(nameof(locale));
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteFieldTable(PeerProperties)
                .WriteShortString(Mechanism)
                .WriteLongString(Response)
                .WriteShortString(Locale);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ConnectionStartOk result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadFieldTable(out var peerProperties, out surplus) &&
                surplus.ReadShortString(out var mechanism, out surplus) &&
                surplus.ReadLongString(out var response, out surplus) &&
                surplus.ReadShortString(out var locale, out surplus))
            {
                result = new ConnectionStartOk(peerProperties, mechanism, response, locale);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
