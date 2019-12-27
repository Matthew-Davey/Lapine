namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using Bogus;
    using Xunit;

    public class BasicDeliverTests : Faker {
        BasicDeliver RandomSubject => new BasicDeliver(
            consumerTag : Random.Word(),
            deliveryTag : Random.ULong(),
            redelivered : Random.Bool(),
            exchangeName: Random.Word()
        );

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new ArrayBufferWriter<Byte>(8);
            var value  = RandomSubject;

            value.Serialize(buffer);
            BasicDeliver.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.ConsumerTag, actual: deserialized.ConsumerTag);
            Assert.Equal(expected: value.DeliveryTag, actual: deserialized.DeliveryTag);
            Assert.Equal(expected: value.ExchangeName, actual: deserialized.ExchangeName);
            Assert.Equal(expected: value.Redelivered, actual: deserialized.Redelivered);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = BasicDeliver.Deserialize(new Byte[0], out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            BasicDeliver.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
