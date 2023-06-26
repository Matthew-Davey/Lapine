namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

using static System.String;

record struct ConnectionStart((Byte Major, Byte Minor) Version, IReadOnlyDictionary<String, Object> ServerProperties, IList<String> Mechanisms, IList<String> Locales) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x0A);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt8(Version.Major)
            .WriteUInt8(Version.Minor)
            .WriteFieldTable(ServerProperties)
            .WriteLongString(Join(' ', Mechanisms))
            .WriteLongString(Join(' ', Locales));

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConnectionStart? result) {
        if (buffer.ReadUInt8(out var major) &&
            buffer.ReadUInt8(out var minor) &&
            buffer.ReadFieldTable(out var serverProperties) &&
            buffer.ReadLongString(out var mechanisms) &&
            buffer.ReadLongString(out var locales))
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

record struct ConnectionStartOk(IReadOnlyDictionary<String, Object> PeerProperties, String Mechanism, String Response, String Locale) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x0B);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteFieldTable(PeerProperties)
            .WriteShortString(Mechanism)
            .WriteLongString(Response)
            .WriteShortString(Locale);

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConnectionStartOk? result) {
        if (buffer.ReadFieldTable(out var peerProperties) &&
            buffer.ReadShortString(out var mechanism) &&
            buffer.ReadLongString(out var response) &&
            buffer.ReadShortString(out var locale))
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
