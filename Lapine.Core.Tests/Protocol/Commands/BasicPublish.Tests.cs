namespace Lapine.Protocol.Commands {
    using System;
    using Bogus;
    using Xunit;

    public class BasicPublishTests : Faker {
        BasicPublish RandomSubject => new (
            exchangeName: Random.Word(),
            routingKey  : Random.Word(),
            mandatory   : Random.Bool(),
            immediate   : Random.Bool()
        );

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new MemoryBufferWriter<Byte>(8);
            var value  = RandomSubject;

            value.Serialize(buffer);
            BasicPublish.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.ExchangeName, actual: deserialized.ExchangeName);
            Assert.Equal(expected: value.Immediate, actual: deserialized.Immediate);
            Assert.Equal(expected: value.Mandatory, actual: deserialized.Mandatory);
            Assert.Equal(expected: value.RoutingKey, actual: deserialized.RoutingKey);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = BasicPublish.Deserialize(Span<Byte>.Empty, out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            BasicPublish.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
