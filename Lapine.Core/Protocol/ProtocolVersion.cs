namespace Lapine.Protocol {
    using System;
    using System.Buffers;

    public readonly struct ProtocolVersion {
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

        public void Serialize(IBufferWriter<Byte> writer) {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));

            var span = writer.GetSpan(sizeHint: 3);
            span[0] = _major;
            span[1] = _minor;
            span[2] = _revision;

            writer.Advance(3);
        }

        public static Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ProtocolVersion result, out ReadOnlySpan<Byte> remaining) {
            if (buffer.ReadUInt8(out var major, out remaining) &&
                remaining.ReadUInt8(out var minor, out remaining) &&
                remaining.ReadUInt8(out var revision, out remaining))
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
