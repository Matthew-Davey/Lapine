namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using Bogus;
    using Xunit;

    public class BasicRejectTests : Faker {
        BasicReject RandomSubject => new BasicReject(
            deliveryTag: Random.ULong(),
            requeue    : Random.Bool()
        );

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new ArrayBufferWriter<Byte>(8);
            var value  = RandomSubject;

            value.Serialize(buffer);
            BasicReject.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.DeliveryTag, actual: deserialized.DeliveryTag);
            Assert.Equal(expected: value.ReQueue, actual: deserialized.ReQueue);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = BasicReject.Deserialize(new Byte[0], out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            BasicReject.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
