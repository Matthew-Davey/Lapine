namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

using static System.String;

readonly record struct ConnectionStart((Byte Major, Byte Minor) Version, IReadOnlyDictionary<String, Object> ServerProperties, IList<String> Mechanisms, IList<String> Locales) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x0A);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt8(Version.Major)
            .WriteUInt8(Version.Minor)
            .WriteFieldTable(ServerProperties)
            .WriteLongString(Join(' ', Mechanisms))
            .WriteLongString(Join(' ', Locales));

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ConnectionStart? result) {
        if (BufferExtensions.ReadUInt8(ref buffer, out var major) &&
            BufferExtensions.ReadUInt8(ref buffer, out var minor) &&
            BufferExtensions.ReadFieldTable(ref buffer, out var serverProperties) &&
            BufferExtensions.ReadLongString(ref buffer, out var mechanisms) &&
            BufferExtensions.ReadLongString(ref buffer, out var locales))
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

readonly record struct ConnectionStartOk(IReadOnlyDictionary<String, Object> PeerProperties, String Mechanism, String Response, String Locale) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x0B);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteFieldTable(PeerProperties)
            .WriteShortString(Mechanism)
            .WriteLongString(Response)
            .WriteShortString(Locale);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ConnectionStartOk? result) {
        if (BufferExtensions.ReadFieldTable(ref buffer, out var peerProperties) &&
            BufferExtensions.ReadShortString(ref buffer, out var mechanism) &&
            BufferExtensions.ReadLongString(ref buffer, out var response) &&
            BufferExtensions.ReadShortString(ref buffer, out var locale))
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
