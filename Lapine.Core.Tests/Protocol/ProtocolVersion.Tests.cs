namespace Lapine.Protocol {
    using System;
    using System.Buffers;
    using Bogus;
    using Xunit;

    public class ProtocolVersionTests : Faker {
        [Fact]
        public void SerializedSizeIsThreeBytes() {
            var value  = new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte());
            var buffer = new ArrayBufferWriter<Byte>(3);

            value.Serialize(buffer);

            Assert.Equal(expected: 3, actual: buffer.WrittenCount);
        }

        [Fact]
        public void SerializationIsSymmetric() {
            var value  = new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte());
            var buffer = new ArrayBufferWriter<Byte>(3);

            value.Serialize(buffer);
            ProtocolVersion.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value, actual: deserialized);
        }
    }
}
