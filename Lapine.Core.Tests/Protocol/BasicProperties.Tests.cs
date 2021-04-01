namespace Lapine.Protocol {
    using System;
    using System.Buffers;
    using Bogus;
    using Xunit;

    public class BasicPropertiesTests : Faker {
        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new ArrayBufferWriter<Byte>();
            var value =  BasicProperties.Empty with {
                AppId           = Random.Utf16String(),
                ClusterId       = Random.Utf16String(),
                ContentType     = Random.Utf16String(),
                ContentEncoding = Random.Utf16String(),
                CorrelationId   = Random.Utf16String(),
                DeliveryMode    = Random.Byte(),
                Expiration      = Random.Utf16String(),
                MessageId       = Random.Utf16String(),
                Priority        = Random.Byte(),
                ReplyTo         = Random.Utf16String(),
                Timestamp       = Random.ULong(),
                Type            = Random.Utf16String(),
                UserId          = Random.Utf16String()
            };

            value.Serialize(buffer);
            BasicProperties.Deserialize(buffer.WrittenSpan, out var deserialized, out var _);

            Assert.Equal(expected: value, actual: deserialized);
        }

        [Fact]
        public void DeserializationFailsWithUnsufficientData() {
            var result = BasicProperties.Deserialize(Array.Empty<Byte>(), out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value =  BasicProperties.Empty with {
                AppId           = Random.Utf16String(),
                ClusterId       = Random.Utf16String(),
                ContentType     = Random.Utf16String(),
                ContentEncoding = Random.Utf16String(),
                CorrelationId   = Random.Utf16String(),
                DeliveryMode    = Random.Byte(),
                Expiration      = Random.Utf16String(),
                MessageId       = Random.Utf16String(),
                Priority        = Random.Byte(),
                ReplyTo         = Random.Utf16String(),
                Timestamp       = Random.ULong(),
                Type            = Random.Utf16String(),
                UserId          = Internet.UserName()
            };
            var extra = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            BasicProperties.Deserialize(buffer.WrittenSpan, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
