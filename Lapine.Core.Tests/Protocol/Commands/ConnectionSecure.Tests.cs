namespace Lapine.Protocol.Commands;

public class ConnectionSecureTests : Faker {
    ConnectionSecure RandomSubject => new (Challenge: Random.AlphaNumeric(Random.Number(1, Int16.MaxValue)));

    [Fact]
    public void SerializationIsSymmetric() {
        var buffer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(buffer);
        ConnectionSecure.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

        Assert.Equal(expected: value.Challenge, actual: deserialized?.Challenge);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var result = ConnectionSecure.Deserialize(Span<Byte>.Empty, out var _, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        ConnectionSecure.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}

public class ConnectionSecureOkTests : Faker {
    ConnectionSecureOk RandomSubject => new (Response: Random.AlphaNumeric(Random.Number(1, Int16.MaxValue)));

    [Fact]
    public void SerializationIsSymmetric() {
        var buffer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(buffer);
        ConnectionSecureOk.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

        Assert.Equal(expected: value.Response, actual: deserialized?.Response);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var result = ConnectionSecureOk.Deserialize(Span<Byte>.Empty, out var _, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        ConnectionSecureOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}
