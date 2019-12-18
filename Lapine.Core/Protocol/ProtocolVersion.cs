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

        public static void Deserialize(in ReadOnlySpan<Byte> buffer, out ProtocolVersion result) {
            if (buffer.Length < 3)
                throw new ArgumentException(nameof(buffer));

            result = new ProtocolVersion(in buffer[0], in buffer[1], in buffer[2]);
        }
    }
}
