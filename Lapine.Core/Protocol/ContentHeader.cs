namespace Lapine.Protocol {
    using System;
    using System.Buffers;

    public struct ContentHeader : ISerializable {
        readonly UInt16 _classId;
        readonly UInt64 _bodySize;
        readonly BasicProperties _properties;

        public ContentHeader(UInt16 classId, UInt64 bodySize, BasicProperties properties) {
            _classId    = classId;
            _bodySize   = bodySize;
            _properties = properties;
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt16BE(_classId)
                .WriteUInt16BE(0) // weight
                .WriteUInt64BE(_bodySize)
                .WriteSerializable(_properties);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ContentHeader result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt16BE(out var classId, out surplus) &&
                surplus.ReadUInt16BE(out _, out surplus) &&
                surplus.ReadUInt64BE(out var bodySize, out surplus) &&
                BasicProperties.Deserialize(surplus, out var basicProperties, out surplus)) {
                    result = new ContentHeader(classId, bodySize, basicProperties);
                    return true;
                }
            else {
                result = default;
                return false;
            }
        }
    }
}
