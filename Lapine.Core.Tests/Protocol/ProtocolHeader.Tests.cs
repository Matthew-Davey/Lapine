namespace Lapine.Protocol {
    using System;
    using System.Buffers;
    using Bogus;
    using Xunit;

    public class ProtocolHeaderTests : Faker {
        [Fact]
        public void SerializedSizeIsEightBytes() {
            var value  = new ProtocolHeader(Random.Chars(count: 4), Random.Byte(), new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte()));
            var buffer = new ArrayBufferWriter<Byte>(8);

            value.Serialize(buffer);

            Assert.Equal(expected: 8, actual: buffer.WrittenCount);
        }

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new ArrayBufferWriter<Byte>(8);
            var value  = new ProtocolHeader(Random.Chars(count: 4), Random.Byte(), new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte()));

            value.Serialize(buffer);
            ProtocolHeader.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value, actual: deserialized);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = ProtocolHeader.Deserialize(new Byte[0], out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = new ProtocolHeader(Random.Chars(count: 4), Random.Byte(), new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte()));
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>(12);

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ProtocolHeader.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
