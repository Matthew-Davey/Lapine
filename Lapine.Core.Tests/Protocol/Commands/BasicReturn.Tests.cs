namespace Lapine.Protocol.Commands;

public class BasicReturnTests : Faker {
    BasicReturn RandomSubject => new (
        ReplyCode   : Random.UShort(),
        ReplyText   : Random.Word(),
        ExchangeName: Random.Word(),
        RoutingKey  : Random.Word()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var buffer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(buffer);
        BasicReturn.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

        Assert.Equal(expected: value.ExchangeName, actual: deserialized?.ExchangeName);
        Assert.Equal(expected: value.ReplyCode, actual: deserialized?.ReplyCode);
        Assert.Equal(expected: value.ReplyText, actual: deserialized?.ReplyText);
        Assert.Equal(expected: value.RoutingKey, actual: deserialized?.RoutingKey);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var result = BasicReturn.Deserialize(Span<Byte>.Empty, out var _, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        BasicReturn.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}
