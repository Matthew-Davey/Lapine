namespace Lapine.Protocol.Commands {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bogus;
    using Xunit;

    public class BasicConsumeTests : Faker {
        BasicConsume RandomSubject => new (
            QueueName   : Random.Word(),
            ConsumerTag : Random.Word(),
            NoLocal     : Random.Bool(),
            NoAck       : Random.Bool(),
            Exclusive   : Random.Bool(),
            NoWait      : Random.Bool(),
            Arguments   : new Dictionary<String, Object> {{ Random.Word(), Random.UInt() }}
        );

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new MemoryBufferWriter<Byte>(8);
            var value  = RandomSubject;

            value.Serialize(buffer);
            BasicConsume.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.Arguments.ToList(), actual: deserialized?.Arguments.ToList());
            Assert.Equal(expected: value.ConsumerTag, actual: deserialized?.ConsumerTag);
            Assert.Equal(expected: value.Exclusive, actual: deserialized?.Exclusive);
            Assert.Equal(expected: value.NoAck, actual: deserialized?.NoAck);
            Assert.Equal(expected: value.NoLocal, actual: deserialized?.NoLocal);
            Assert.Equal(expected: value.NoWait, actual: deserialized?.NoWait);
            Assert.Equal(expected: value.QueueName, actual: deserialized?.QueueName);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = BasicConsume.Deserialize(Span<Byte>.Empty, out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            BasicConsume.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }

    public class BasicConsumeOkTests : Faker {
        BasicConsumeOk RandomSubject => new (
            ConsumerTag: Random.Word()
        );

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new MemoryBufferWriter<Byte>();
            var value  = RandomSubject;

            value.Serialize(buffer);
            BasicConsumeOk.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.ConsumerTag, actual: deserialized?.ConsumerTag);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = BasicConsumeOk.Deserialize(Span<Byte>.Empty, out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            BasicConsumeOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
