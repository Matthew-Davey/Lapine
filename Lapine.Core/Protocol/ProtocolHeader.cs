namespace Lapine.Protocol;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

using static System.Text.Encoding;

readonly record struct ProtocolHeader(UInt32 Protocol, Byte ProtocolId, ProtocolVersion Version) : ISerializable {
    static public ProtocolHeader Default =>
        Create("AMQP", 0, ProtocolVersion.Default);

    static public ProtocolHeader Create(in ReadOnlySpan<Char> protocol, in Byte protocolId, in ProtocolVersion version) {
        if (protocol.Length != 4)
            throw new ArgumentException("value must be exactly four characters long", nameof(protocol));

        return new ProtocolHeader(
            Protocol  : BitConverter.ToUInt32(ASCII.GetBytes(protocol.ToArray())),
            ProtocolId: protocolId,
            Version   : version
        );
    }

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt32LE(Protocol)
            .WriteUInt8(ProtocolId)
            .WriteSerializable(Version);

    public static Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ProtocolHeader? result) {
        if (buffer.ReadChars(4, out var protocol) &&
            buffer.ReadUInt8(out var protocolId) &&
            ProtocolVersion.Deserialize(ref buffer, out var version))
        {
            result = Create(protocol, protocolId, version.Value);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
