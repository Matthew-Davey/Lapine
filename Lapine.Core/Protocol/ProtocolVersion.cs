namespace Lapine.Protocol;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct ProtocolVersion(Byte Major, Byte Minor, Byte Revision) : ISerializable {
    static public ProtocolVersion Default =>
        new (0, 9, 1);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt8(Major)
            .WriteUInt8(Minor)
            .WriteUInt8(Revision);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ProtocolVersion? result) {
        if (BufferExtensions.ReadUInt8(ref buffer, out var major) &&
            BufferExtensions.ReadUInt8(ref buffer, out var minor) &&
            BufferExtensions.ReadUInt8(ref buffer, out var revision))
        {
            result = new ProtocolVersion(major, minor, revision);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
