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

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = ProtocolVersion.Deserialize(Array.Empty<Byte>(), out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte());
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>(7);

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ProtocolVersion.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
