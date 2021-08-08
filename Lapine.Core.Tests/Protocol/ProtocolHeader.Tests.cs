namespace Lapine.Protocol {
    using System;
    using Bogus;
    using Xunit;

    public class ProtocolHeaderTests : Faker {
        [Fact]
        public void SerializedSizeIsEightBytes() {
            var value  = ProtocolHeader.Create(Random.Chars(count: 4), Random.Byte(), new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte()));
            var buffer = new MemoryBufferWriter<Byte>(8);

            value.Serialize(buffer);

            Assert.Equal(expected: 8, actual: buffer.WrittenCount);
        }

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new MemoryBufferWriter<Byte>(8);
            var value  = ProtocolHeader.Create(Random.Chars(count: 4), Random.Byte(), new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte()));

            value.Serialize(buffer);
            ProtocolHeader.Deserialize(buffer.WrittenSpan, out var deserialized, out var _);

            Assert.Equal(expected: value, actual: deserialized);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = ProtocolHeader.Deserialize(Span<Byte>.Empty, out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = ProtocolHeader.Create(Random.Chars(count: 4), Random.Byte(), new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte()));
            var extra  = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>(12);

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ProtocolHeader.Deserialize(buffer.WrittenSpan, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
