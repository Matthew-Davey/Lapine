namespace Lapine.Protocol;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct ContentHeader(UInt16 ClassId, UInt64 BodySize, BasicProperties Properties) : ISerializable {
    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(ClassId)
            .WriteUInt16BE(0) // weight
            .WriteUInt64BE(BodySize)
            .WriteSerializable(Properties);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ContentHeader? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out var classId) &&
            BufferExtensions.ReadUInt16BE(ref buffer, out _) &&
            BufferExtensions.ReadUInt64BE(ref buffer, out var bodySize) &&
            BasicProperties.Deserialize(ref buffer, out var basicProperties)) {
                result = new ContentHeader(classId, bodySize, basicProperties.Value);
                return true;
            }
        else {
            result = default;
            return false;
        }
    }
}
