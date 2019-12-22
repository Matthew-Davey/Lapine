namespace Lapine.Protocol {
    using System;
    using System.Buffers;
    using Bogus;
    using Xunit;

    public class RawFrameTests : Faker {
        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new ArrayBufferWriter<Byte>();
            var value  = new RawFrame(Random.Enum<FrameType>(), Random.UShort(), Random.Bytes(Random.UShort()));

            value.Serialize(buffer);
            RawFrame.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.Channel, actual: deserialized.Channel);
            Assert.Equal(expected: value.Payload.ToArray(), actual: deserialized.Payload.ToArray());
            Assert.Equal(expected: value.Size, actual: deserialized.Size);
            Assert.Equal(expected: value.Type, actual: deserialized.Type);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = RawFrame.Deserialize(new Byte[0], out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationFailsWithInvalidFrameType() {
            var buffer = new ArrayBufferWriter<Byte>(8);
            var value  = new RawFrame(Random.Enum<FrameType>(), Random.UShort(), Random.Bytes(Random.UShort()));

            value.Serialize(buffer);
            var modifiedBuffer = buffer.WrittenMemory.ToArray();
            modifiedBuffer[0] = Random.Byte(min: 10);

            Assert.Throws<ProtocolErrorException>(() => RawFrame.Deserialize(modifiedBuffer.AsSpan(), out var _, out var _));
        }

        [Fact]
        public void DeserializationFailsWithInvalidFrameTerminator() {
            var buffer = new ArrayBufferWriter<Byte>(8);
            var value  = new RawFrame(Random.Enum<FrameType>(), Random.UShort(), Random.Bytes(Random.UShort()));

            value.Serialize(buffer);
            var modifiedBuffer = buffer.WrittenMemory.ToArray();
            modifiedBuffer[modifiedBuffer.Length -1] = 0x00;

            Assert.Throws<FramingErrorException>(() => RawFrame.Deserialize(modifiedBuffer.AsSpan(), out var _, out var _));
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = new RawFrame(Random.Enum<FrameType>(), Random.UShort(), Random.Bytes(Random.UShort()));
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>(12);

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            RawFrame.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
