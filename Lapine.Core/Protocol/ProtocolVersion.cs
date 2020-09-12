namespace Lapine.Protocol {
    using System;
    using System.Buffers;

    readonly struct ProtocolVersion : ISerializable {
        readonly Byte _major;
        readonly Byte _minor;
        readonly Byte _revision;

        static public ProtocolVersion Default =>
            new ProtocolVersion(0, 9, 1);

        public ProtocolVersion(in Byte major, in Byte minor, in Byte revision) {
            _major    = major;
            _minor    = minor;
            _revision = revision;
        }

        public Byte Major => _major;
        public Byte Minor => _minor;
        public Byte Revision => _revision;

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt8(_major)
                .WriteUInt8(_minor)
                .WriteUInt8(_revision);

        public static Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ProtocolVersion result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt8(out var major, out surplus) &&
                surplus.ReadUInt8(out var minor, out surplus) &&
                surplus.ReadUInt8(out var revision, out surplus))
            {
                result = new ProtocolVersion(in major, in minor, in revision);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
