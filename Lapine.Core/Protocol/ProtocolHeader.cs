namespace Lapine.Protocol {
    using System;
    using System.Buffers;

    using static System.Buffers.Binary.BinaryPrimitives;
    using static System.Text.Encoding;

    public readonly struct ProtocolHeader {
        readonly UInt32 _literalAmqp;
        readonly Byte _protocolId;
        readonly ProtocolVersion _version;

        static public ProtocolHeader Default =>
            new ProtocolHeader(0, ProtocolVersion.Default);

        public ProtocolHeader(in Byte protocolId, in ProtocolVersion version) {
            _literalAmqp = BitConverter.ToUInt32(ASCII.GetBytes("AMQP"), 0);
            _protocolId  = protocolId;
            _version     = version;
        }

        public void Serialize(IBufferWriter<Byte> writer) {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));

            var span = writer.GetSpan(sizeHint: 5);

            WriteUInt32LittleEndian(destination: span, value: _literalAmqp);
            span[5] = _protocolId;
            writer.Advance(5);

            _version.Serialize(writer);
        }

        public static void Deserialize(in ReadOnlySpan<Byte> buffer, out ProtocolHeader result) {
            if (buffer.Length < 8)
                throw new ArgumentException(nameof(buffer));

            ProtocolVersion.Deserialize(buffer.Slice(5), out var version);

            result = new ProtocolHeader(in buffer[5], in version);
        }
    }
}
