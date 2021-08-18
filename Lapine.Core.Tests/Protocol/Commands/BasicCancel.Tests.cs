namespace Lapine.Protocol.Commands {
    using System;
    using Bogus;
    using Xunit;

    public class BasicCancelTests : Faker {
        BasicCancel RandomSubject => new (
            ConsumerTag : Random.Word(),
            NoWait      : Random.Bool()
        );

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new MemoryBufferWriter<Byte>(8);
            var value  = RandomSubject;

            value.Serialize(buffer);
            BasicCancel.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.ConsumerTag, actual: deserialized?.ConsumerTag);
            Assert.Equal(expected: value.NoWait, actual: deserialized?.NoWait);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = BasicCancel.Deserialize(Span<Byte>.Empty, out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            BasicCancel.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }

    public class BasicCancelOkTests : Faker {
        BasicCancelOk RandomSubject => new (
            ConsumerTag: Random.Word()
        );

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new MemoryBufferWriter<Byte>();
            var value  = RandomSubject;

            value.Serialize(buffer);
            BasicCancelOk.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.ConsumerTag, actual: deserialized?.ConsumerTag);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = BasicCancelOk.Deserialize(Span<Byte>.Empty, out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            BasicCancelOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
