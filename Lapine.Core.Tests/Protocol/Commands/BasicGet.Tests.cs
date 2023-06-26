namespace Lapine.Protocol.Commands;

public class BasicGetTests : Faker {
    BasicGet RandomSubject => new (
        QueueName: Random.Word(),
        NoAck    : Random.Bool()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(writer);

        var buffer = writer.WrittenSpan;

        BasicGet.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.NoAck, actual: deserialized?.NoAck);
        Assert.Equal(expected: value.QueueName, actual: deserialized?.QueueName);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = BasicGet.Deserialize(ref buffer, out var _);

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

        BasicGet.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}

public class BasicGetEmptyTests : Faker {
    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = new BasicGetEmpty();
        var extra  = Random.UInt();
        var writer = new MemoryBufferWriter<Byte>();

        writer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenSpan;

        BasicGetEmpty.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}

public class BasicGetOkTests : Faker {
    BasicGetOk RandomSubject => new (
        DeliveryTag : Random.ULong(),
        Redelivered : Random.Bool(),
        ExchangeName: Random.Word(),
        RoutingKey  : Random.Word(),
        MessageCount: Random.UInt()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(writer);

        var buffer = writer.WrittenSpan;

        BasicGetOk.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.DeliveryTag, actual: deserialized?.DeliveryTag);
        Assert.Equal(expected: value.ExchangeName, actual: deserialized?.ExchangeName);
        Assert.Equal(expected: value.MessageCount, actual: deserialized?.MessageCount);
        Assert.Equal(expected: value.Redelivered, actual: deserialized?.Redelivered);
        Assert.Equal(expected: value.RoutingKey, actual: deserialized?.RoutingKey);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = BasicGetOk.Deserialize(ref buffer, out var _);

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

        BasicGetOk.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}
