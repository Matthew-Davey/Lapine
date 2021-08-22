namespace Lapine.Protocol;

using System;
using System.Buffers;

readonly record struct ContentHeader(UInt16 ClassId, UInt64 BodySize, BasicProperties Properties) : ISerializable {
    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt16BE(ClassId)
            .WriteUInt16BE(0) // weight
            .WriteUInt64BE(BodySize)
            .WriteSerializable(Properties);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ContentHeader result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt16BE(out var classId, out surplus) &&
            surplus.ReadUInt16BE(out _, out surplus) &&
            surplus.ReadUInt64BE(out var bodySize, out surplus) &&
            BasicProperties.Deserialize(surplus, out var basicProperties, out surplus)) {
                result = new ContentHeader(classId, bodySize, basicProperties.Value);
                return true;
            }
        else {
            result = default;
            return false;
        }
    }
}
