namespace Lapine.Protocol {
    using System;
    using System.Buffers;
    using Bogus;
    using Xunit;

    public class FrameHeaderTests : Faker {
        [Fact]
        public void SerializedSizeIsSevenBytes() {
            var value  = new FrameHeader(Random.Enum<FrameType>(), Random.UShort(), Random.UInt());
            var buffer = new ArrayBufferWriter<Byte>(7);

            value.Serialize(buffer);

            Assert.Equal(expected: 7, actual: buffer.WrittenCount);
        }

        [Fact]
        public void SerializationIsSymmetric() {
            var value  = new FrameHeader(Random.Enum<FrameType>(), Random.UShort(), Random.UInt());
            var buffer = new ArrayBufferWriter<Byte>(7);

            value.Serialize(buffer);
            FrameHeader.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value, actual: deserialized);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var value  = new FrameHeader(Random.Enum<FrameType>(), Random.UShort(), Random.UInt());
            var result = FrameHeader.Deserialize(new Byte[0], out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = new FrameHeader(Random.Enum<FrameType>(), Random.UShort(), Random.UInt());
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>(11);

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            FrameHeader.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
