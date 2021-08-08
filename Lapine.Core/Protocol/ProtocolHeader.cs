namespace Lapine.Protocol {
    using System;
    using System.Buffers;

    using static System.Text.Encoding;

    readonly record struct ProtocolHeader(UInt32 Protocol, Byte ProtocolId, ProtocolVersion Version) : ISerializable {
        static public ProtocolHeader Default =>
            Create("AMQP", 0, ProtocolVersion.Default);

        static public ProtocolHeader Create(in ReadOnlySpan<Char> protocol, in Byte protocolId, in ProtocolVersion version) {
            if (protocol.Length != 4)
                throw new ArgumentException("value must be exactly four characters long", nameof(protocol));

            return new ProtocolHeader(
                Protocol  : BitConverter.ToUInt32(ASCII.GetBytes(protocol.ToArray())),
                ProtocolId: protocolId,
                Version   : version
            );
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer.WriteUInt32LE(Protocol)
                .WriteUInt8(ProtocolId)
                .WriteSerializable(Version);

        public static Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ProtocolHeader result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadChars(4, out var protocol, out surplus) &&
                surplus.ReadUInt8(out var protocolId, out surplus) &&
                ProtocolVersion.Deserialize(surplus, out var version, out surplus))
            {
                result = Create(protocol, protocolId, version);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }
}
