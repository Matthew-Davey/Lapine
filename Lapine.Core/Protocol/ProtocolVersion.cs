namespace Lapine.Protocol;

using System;
using System.Buffers;

readonly record struct ProtocolVersion(Byte Major, Byte Minor, Byte Revision) : ISerializable {
    static public ProtocolVersion Default =>
        new (0, 9, 1);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt8(Major)
            .WriteUInt8(Minor)
            .WriteUInt8(Revision);

    public static Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ProtocolVersion result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt8(out var major, out surplus) &&
            surplus.ReadUInt8(out var minor, out surplus) &&
            surplus.ReadUInt8(out var revision, out surplus))
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
