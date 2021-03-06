namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using Bogus;
    using Xunit;

    public class ChannelCloseTests : Faker {
        ChannelClose RandomSubject => new (
            replyCode    : Random.UShort(),
            replyText    : Lorem.Sentence(),
            failingMethod: (Random.UShort(), Random.UShort())
        );

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new ArrayBufferWriter<Byte>();
            var value  = RandomSubject;

            value.Serialize(buffer);
            ChannelClose.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.FailingMethod, actual: deserialized.FailingMethod);
            Assert.Equal(expected: value.ReplyCode, actual: deserialized.ReplyCode);
            Assert.Equal(expected: value.ReplyText, actual: deserialized.ReplyText);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = ChannelClose.Deserialize(Array.Empty<Byte>(), out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ChannelClose.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }

    public class ChannelCloseOkTests : Faker {
        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = new ChannelCloseOk();
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ChannelCloseOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
