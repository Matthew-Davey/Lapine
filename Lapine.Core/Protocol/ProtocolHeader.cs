namespace Lapine.Protocol {
    using System;
    using System.Buffers;

    using static System.Buffers.Binary.BinaryPrimitives;
    using static System.Text.Encoding;

    public readonly struct ProtocolHeader {
        readonly UInt32 _protocol;
        readonly Byte _protocolId;
        readonly ProtocolVersion _version;

        static public ProtocolHeader Default =>
            new ProtocolHeader("AMQP", 0, ProtocolVersion.Default);

        public ProtocolHeader(in String protocol, in Byte protocolId, in ProtocolVersion version) {
            if (protocol.Length != 4)
                throw new ArgumentException(nameof(protocol), "value must be exactly four characters long");

            _protocol   = BitConverter.ToUInt32(ASCII.GetBytes(protocol), 0);
            _protocolId = protocolId;
            _version    = version;
        }

        public void Serialize(IBufferWriter<Byte> writer) {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));

            var span = writer.GetSpan(sizeHint: 5);

            WriteUInt32LittleEndian(destination: span, value: _protocol);
            span[5] = _protocolId;
            writer.Advance(5);

            _version.Serialize(writer);
        }

        public static void Deserialize(in ReadOnlySpan<Byte> buffer, out ProtocolHeader result) {
            if (buffer.Length < 8)
                throw new ArgumentException(nameof(buffer));

            var protocol = ASCII.GetString(buffer.Slice(0, 4));

            ProtocolVersion.Deserialize(buffer.Slice(5, 3), out var version);

            result = new ProtocolHeader(in protocol, in buffer[5], in version);
        }
    }
}
