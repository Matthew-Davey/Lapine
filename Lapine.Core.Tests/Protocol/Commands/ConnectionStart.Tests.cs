namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Linq;
    using Bogus;
    using Xunit;

    public class ConnectionStartTests : Faker {
        ConnectionStart RandomSubject => new ConnectionStart(
            version         : (Random.Byte(), Random.Byte()),
            serverProperties: Random.String2(minLength: 1, maxLength: Int16.MaxValue),
            mechanisms      : Make(Random.Number(1, 8), () => Random.AlphaNumeric(Random.Number(4, 24))).ToArray(),
            locales         : Make(Random.Number(1, 8), () => Random.RandomLocale()).ToArray());

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new ArrayBufferWriter<Byte>(8);
            var value  = RandomSubject;

            value.Serialize(buffer);
            ConnectionStart.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.Locales, actual: deserialized.Locales);
            Assert.Equal(expected: value.Mechanisms, actual: deserialized.Mechanisms);
            Assert.Equal(expected: value.ServerProperties, actual: deserialized.ServerProperties);
            Assert.Equal(expected: value.Version, actual: deserialized.Version);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = ConnectionStart.Deserialize(new Byte[0], out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ConnectionStart.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
