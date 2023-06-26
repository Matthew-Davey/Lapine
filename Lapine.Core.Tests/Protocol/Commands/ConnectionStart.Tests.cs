namespace Lapine.Protocol.Commands;

public class ConnectionStartTests : Faker {
    ConnectionStart RandomSubject => new (
        Version         : (Random.Byte(), Random.Byte()),
        ServerProperties: new Dictionary<String, Object> { { Random.Word(), Random.UInt() } },
        Mechanisms      : Make(Random.Number(1, 8), () => Random.AlphaNumeric(Random.Number(4, 24))).ToArray(),
        Locales         : Make(Random.Number(1, 8), () => Random.RandomLocale()).ToArray());

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(writer);
        
        var buffer = writer.WrittenSpan;
        
        ConnectionStart.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.Locales, actual: deserialized?.Locales);
        Assert.Equal(expected: value.Mechanisms, actual: deserialized?.Mechanisms);
        Assert.Equal(expected: value.ServerProperties, actual: deserialized?.ServerProperties);
        Assert.Equal(expected: value.Version, actual: deserialized?.Version);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = ConnectionStart.Deserialize(ref buffer, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var writer = new MemoryBufferWriter<Byte>();

        writer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenSpan;

        ConnectionStart.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}

public class ConnectionStartOkTests : Faker {
    ConnectionStartOk RandomSubject => new (
        PeerProperties: new Dictionary<String, Object> { { Random.Word(), Random.UInt() } },
        Mechanism     : Random.AlphaNumeric(Random.Number(4, 24)),
        Response      : Random.AlphaNumeric(Random.Number(4, Int16.MaxValue)),
        Locale        : Random.RandomLocale());

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(writer);
        
        var buffer = writer.WrittenSpan;
        
        ConnectionStartOk.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.PeerProperties, actual: deserialized?.PeerProperties);
        Assert.Equal(expected: value.Mechanism, actual: deserialized?.Mechanism);
        Assert.Equal(expected: value.Response, actual: deserialized?.Response);
        Assert.Equal(expected: value.Locale, actual: deserialized?.Locale);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = ConnectionStartOk.Deserialize(ref buffer, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var writer = new MemoryBufferWriter<Byte>();

        writer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenSpan;

        ConnectionStartOk.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}
