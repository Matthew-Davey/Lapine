namespace Lapine.Protocol.Commands;

public class BasicRejectTests : Faker {
    BasicReject RandomSubject => new (
        DeliveryTag: Random.ULong(),
        ReQueue    : Random.Bool()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var buffer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(buffer);
        BasicReject.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

        Assert.Equal(expected: value.DeliveryTag, actual: deserialized?.DeliveryTag);
        Assert.Equal(expected: value.ReQueue, actual: deserialized?.ReQueue);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var result = BasicReject.Deserialize(Span<Byte>.Empty, out var _, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        BasicReject.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}
