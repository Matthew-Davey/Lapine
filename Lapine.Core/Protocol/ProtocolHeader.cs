namespace Lapine.Protocol
{
    using System;
    using System.Buffers;

    using static System.Text.Encoding;

    public readonly struct ProtocolHeader : ISerializable {
        readonly UInt32 _protocol;
        readonly Byte _protocolId;
        readonly ProtocolVersion _version;

        static public ProtocolHeader Default =>
            new ProtocolHeader("AMQP", 0, ProtocolVersion.Default);

        public ProtocolHeader(in ReadOnlySpan<Char> protocol, in Byte protocolId, in ProtocolVersion version) {
            if (protocol.Length != 4)
                throw new ArgumentException(nameof(protocol), "value must be exactly four characters long");

            _protocol   = BitConverter.ToUInt32(ASCII.GetBytes(protocol.ToArray()));
            _protocolId = protocolId;
            _version    = version;
        }

        public ReadOnlySpan<Char> Protocol =>
            ASCII.GetString(BitConverter.GetBytes(_protocol)).AsSpan();
        public Byte ProtocolId => _protocolId;
        public ProtocolVersion Version => _version;

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt32LE(_protocol)
                .WriteUInt8(_protocolId)
                .WriteSerializable(_version);

        public static Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ProtocolHeader result, out ReadOnlySpan<Byte> remaining) {
            if (buffer.ReadChars(4, out var protocol, out remaining) &&
                remaining.ReadUInt8(out var protocolId, out remaining) &&
                ProtocolVersion.Deserialize(remaining, out var version, out remaining))
            {
                result = new ProtocolHeader(protocol, protocolId, version);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
