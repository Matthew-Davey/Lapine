namespace Lapine.Protocol.Commands {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bogus;
    using Xunit;

    public class ExchangeDeclareTests : Faker {
        ExchangeDeclare RandomSubject => new (
            exchangeName: Random.Word(),
            exchangeType: Random.Word(),
            passive     : Random.Bool(),
            durable     : Random.Bool(),
            autoDelete  : Random.Bool(),
            @internal   : Random.Bool(),
            noWait      : Random.Bool(),
            arguments   : new Dictionary<String, Object> {{ Random.Word(), Random.UInt() }}
        );

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new MemoryBufferWriter<Byte>(8);
            var value  = RandomSubject;

            value.Serialize(buffer);
            ExchangeDeclare.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.Arguments.ToList(), actual: deserialized.Arguments.ToList());
            Assert.Equal(expected: value.AutoDelete, actual: deserialized.AutoDelete);
            Assert.Equal(expected: value.Durable, actual: deserialized.Durable);
            Assert.Equal(expected: value.ExchangeName, actual: deserialized.ExchangeName);
            Assert.Equal(expected: value.ExchangeType, actual: deserialized.ExchangeType);
            Assert.Equal(expected: value.Internal, actual: deserialized.Internal);
            Assert.Equal(expected: value.NoWait, actual: deserialized.NoWait);
            Assert.Equal(expected: value.Passive, actual: deserialized.Passive);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = ExchangeDeclare.Deserialize(Span<Byte>.Empty, out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ExchangeDeclare.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }

    public class ExchangeDeclareOkTests : Faker {
        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = new ExchangeDeclareOk();
            var extra  = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ExchangeDeclareOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
