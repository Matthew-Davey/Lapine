namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using Bogus;
    using Xunit;

    public class ConnectionStartOkTests : Faker {
        ConnectionStartOk RandomSubject => new ConnectionStartOk(
            peerProperties: Random.String2(minLength: 1, maxLength: Int16.MaxValue),
            mechanism     : Random.AlphaNumeric(Random.Number(4, 24)),
            response      : Random.AlphaNumeric(Random.Number(4, Int16.MaxValue)),
            locale        : Random.RandomLocale());

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new ArrayBufferWriter<Byte>(8);
            var value  = RandomSubject;

            value.Serialize(buffer);
            ConnectionStartOk.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.PeerProperties, actual: deserialized.PeerProperties);
            Assert.Equal(expected: value.Mechanism, actual: deserialized.Mechanism);
            Assert.Equal(expected: value.Response, actual: deserialized.Response);
            Assert.Equal(expected: value.Locale, actual: deserialized.Locale);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = ConnectionStartOk.Deserialize(new Byte[0], out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ConnectionStartOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
