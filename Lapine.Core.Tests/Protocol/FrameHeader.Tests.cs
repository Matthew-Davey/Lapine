namespace Lapine.Protocol {
    using System;
    using System.Buffers;
    using Bogus;
    using Xunit;

    public class FrameHeaderTests : Faker {
        [Fact]
        public void SerializedSizeIsSevenBytes() {
            var value  = new FrameHeader(Random.Enum<FrameType>(), Random.UShort(0, 32), Random.UInt());
            var buffer = new ArrayBufferWriter<Byte>(7);

            value.Serialize(buffer);

            Assert.Equal(expected: 7, actual: buffer.WrittenCount);
        }

        [Fact]
        public void SerializationIsSymmetric() {
            var value  = new FrameHeader(Random.Enum<FrameType>(), Random.UShort(0, 32), Random.UInt());
            var buffer = new ArrayBufferWriter<Byte>(8);
            value.Serialize(buffer);
            FrameHeader.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value, actual: deserialized);
        }
    }
}
