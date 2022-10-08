namespace Lapine.Protocol.Commands;

public class BasicAckTests : Faker {
    BasicAck RandomSubject => new (
        DeliveryTag: Random.ULong(),
        Multiple   : Random.Bool()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(writer);
        var buffer = writer.WrittenMemory;
        BasicAck.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.DeliveryTag, actual: deserialized?.DeliveryTag);
        Assert.Equal(expected: value.Multiple, actual: deserialized?.Multiple);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlyMemory<Byte>.Empty;
        var result = BasicAck.Deserialize(ref buffer, out _);

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

        BasicAck.Deserialize(ref buffer, out _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer.Span));
    }
}
