namespace Lapine.Protocol.Commands;

public class BasicDeliverTests : Faker {
    BasicDeliver RandomSubject => new (
        ConsumerTag : Random.Word(),
        DeliveryTag : Random.ULong(),
        Redelivered : Random.Bool(),
        ExchangeName: Random.Word()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(writer);
        var buffer = writer.WrittenMemory;

        BasicDeliver.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.ConsumerTag, actual: deserialized?.ConsumerTag);
        Assert.Equal(expected: value.DeliveryTag, actual: deserialized?.DeliveryTag);
        Assert.Equal(expected: value.ExchangeName, actual: deserialized?.ExchangeName);
        Assert.Equal(expected: value.Redelivered, actual: deserialized?.Redelivered);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlyMemory<Byte>.Empty;
        var result = BasicDeliver.Deserialize(ref buffer, out _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var writer = new MemoryBufferWriter<Byte>();

        writer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenMemory;

        BasicDeliver.Deserialize(ref buffer, out _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer.Span));
    }
}
