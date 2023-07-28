namespace Lapine.Protocol.Commands;

public class BasicPublishTests : Faker {
    BasicPublish RandomSubject => new (
        ExchangeName: Random.Word(),
        RoutingKey  : Random.Word(),
        Mandatory   : Random.Bool(),
        Immediate   : Random.Bool()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(writer);

        var buffer = writer.WrittenSpan;

        BasicPublish.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.ExchangeName, actual: deserialized?.ExchangeName);
        Assert.Equal(expected: value.Immediate, actual: deserialized?.Immediate);
        Assert.Equal(expected: value.Mandatory, actual: deserialized?.Mandatory);
        Assert.Equal(expected: value.RoutingKey, actual: deserialized?.RoutingKey);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = BasicPublish.Deserialize(ref buffer, out var _);

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

        BasicPublish.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}
