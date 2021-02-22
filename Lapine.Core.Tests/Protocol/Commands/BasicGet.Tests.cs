namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using Bogus;
    using Xunit;

    public class BasicGetTests : Faker {
        BasicGet RandomSubject => new (
            queueName: Random.Word(),
            noAck    : Random.Bool()
        );

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new ArrayBufferWriter<Byte>(8);
            var value  = RandomSubject;

            value.Serialize(buffer);
            BasicGet.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.NoAck, actual: deserialized.NoAck);
            Assert.Equal(expected: value.QueueName, actual: deserialized.QueueName);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = BasicGet.Deserialize(Array.Empty<Byte>(), out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            BasicGet.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }

    public class BasicGetEmptyTests : Faker {
        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = new BasicGetEmpty();
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            BasicGetEmpty.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }

    public class BasicGetOkTests : Faker {
        BasicGetOk RandomSubject => new (
            deliveryTag : Random.ULong(),
            redelivered : Random.Bool(),
            exchangeName: Random.Word(),
            routingKey  : Random.Word(),
            messageCount: Random.UInt()
        );

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new ArrayBufferWriter<Byte>();
            var value  = RandomSubject;

            value.Serialize(buffer);
            BasicGetOk.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.DeliveryTag, actual: deserialized.DeliveryTag);
            Assert.Equal(expected: value.ExchangeName, actual: deserialized.ExchangeName);
            Assert.Equal(expected: value.MessageCount, actual: deserialized.MessageCount);
            Assert.Equal(expected: value.Redelivered, actual: deserialized.Redelivered);
            Assert.Equal(expected: value.RoutingKey, actual: deserialized.RoutingKey);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = BasicGetOk.Deserialize(Array.Empty<Byte>(), out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            BasicGetOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
