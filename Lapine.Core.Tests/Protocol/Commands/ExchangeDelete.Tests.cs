namespace Lapine.Protocol.Commands {
    using System;
    using Bogus;
    using Xunit;

    public class ExchangeDeleteTests : Faker {
        ExchangeDelete RandomSubject => new (
            exchangeName: Random.Word(),
            ifUnused    : Random.Bool(),
            noWait      : Random.Bool()
        );

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new MemoryBufferWriter<Byte>(8);
            var value  = RandomSubject;

            value.Serialize(buffer);
            ExchangeDelete.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.ExchangeName, actual: deserialized.ExchangeName);
            Assert.Equal(expected: value.IfUnused, actual: deserialized.IfUnused);
            Assert.Equal(expected: value.NoWait, actual: deserialized.NoWait);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = ExchangeDelete.Deserialize(Span<Byte>.Empty, out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ExchangeDelete.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }

    public class ExchangeDeleteOkTests : Faker {
        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = new ExchangeDeleteOk();
            var extra  = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ExchangeDeleteOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
