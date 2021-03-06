namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using Bogus;
    using Xunit;

    public class ConnectionStartTests : Faker {
        ConnectionStart RandomSubject => new (
            version         : (Random.Byte(), Random.Byte()),
            serverProperties: new Dictionary<String, Object> { { Random.Word(), Random.UInt() } },
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
            var result = ConnectionStart.Deserialize(Array.Empty<Byte>(), out var _, out var _);

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

    public class ConnectionStartOkTests : Faker {
        ConnectionStartOk RandomSubject => new (
            peerProperties: new Dictionary<String, Object> { { Random.Word(), Random.UInt() } },
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
            var result = ConnectionStartOk.Deserialize(Array.Empty<Byte>(), out var _, out var _);

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
