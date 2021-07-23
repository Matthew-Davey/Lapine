namespace Lapine.Protocol {
    using System;
    using Bogus;
    using Xunit;

    public class ContentHeaderTests : Faker {
        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new MemoryBufferWriter<Byte>();
            var value = new ContentHeader(
                classId   : Random.UShort(),
                bodySize  : Random.ULong(),
                properties: BasicProperties.Empty with {
                    AppId           = Random.Utf16String(),
                    ClusterId       = Random.Utf16String(),
                    ContentEncoding = Random.Utf16String(),
                    ContentType     = Random.Utf16String(),
                    CorrelationId   = Random.Utf16String(),
                    DeliveryMode    = Random.Byte(),
                    Expiration      = Random.Utf16String(),
                    MessageId       = Random.Utf16String(),
                    Priority        = Random.Byte(),
                    ReplyTo         = Random.Utf16String(),
                    Timestamp       = Random.ULong(),
                    Type            = Random.Utf16String(),
                    UserId          = Random.Utf16String()
                }
            );

            value.Serialize(buffer);
            ContentHeader.Deserialize(buffer.WrittenSpan, out var deserialized, out var _);

            Assert.Equal(expected: value, actual: deserialized);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = ContentHeader.Deserialize(Span<Byte>.Empty, out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value = new ContentHeader(
                classId   : Random.UShort(),
                bodySize  : Random.ULong(),
                properties: BasicProperties.Empty with {
                    AppId           = Random.Utf16String(),
                    ClusterId       = Random.Utf16String(),
                    ContentEncoding = Random.Utf16String(),
                    ContentType     = Random.Utf16String(),
                    CorrelationId   = Random.Utf16String(),
                    DeliveryMode    = Random.Byte(),
                    Expiration      = Random.Utf16String(),
                    MessageId       = Random.Utf16String(),
                    Priority        = Random.Byte(),
                    ReplyTo         = Random.Utf16String(),
                    Timestamp       = Random.ULong(),
                    Type            = Random.Utf16String(),
                    UserId          = Random.Utf16String()
                }
            );
            var extra = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ContentHeader.Deserialize(buffer.WrittenSpan, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
