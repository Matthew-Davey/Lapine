namespace Lapine.Protocol;

public class BasicPropertiesTests : Faker {
    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>();
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

        value.Serialize(writer);
        var buffer = writer.WrittenSpan;
        BasicProperties.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value, actual: deserialized);
    }

    [Fact]
    public void DeserializationFailsWithUnsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = BasicProperties.Deserialize(ref buffer, out var _);

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
        var writer = new MemoryBufferWriter<Byte>();

        writer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenSpan;

        BasicProperties.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}
