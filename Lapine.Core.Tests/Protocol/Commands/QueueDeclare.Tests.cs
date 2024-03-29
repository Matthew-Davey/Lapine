namespace Lapine.Protocol.Commands;

public class QueueDeclareTests : Faker {
    QueueDeclare RandomSubject => new (
        QueueName : Random.Word(),
        Passive   : Random.Bool(),
        Durable   : Random.Bool(),
        Exclusive : Random.Bool(),
        AutoDelete: Random.Bool(),
        NoWait    : Random.Bool(),
        Arguments : new Dictionary<String, Object> {{ Random.Word(), Random.UInt() }}
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(writer);
        
        var buffer = writer.WrittenSpan;
        
        QueueDeclare.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.Arguments.ToList(), actual: deserialized?.Arguments.ToList());
        Assert.Equal(expected: value.AutoDelete, actual: deserialized?.AutoDelete);
        Assert.Equal(expected: value.Durable, actual: deserialized?.Durable);
        Assert.Equal(expected: value.Exclusive, actual: deserialized?.Exclusive);
        Assert.Equal(expected: value.NoWait, actual: deserialized?.NoWait);
        Assert.Equal(expected: value.Passive, actual: deserialized?.Passive);
        Assert.Equal(expected: value.QueueName, actual: deserialized?.QueueName);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = QueueDeclare.Deserialize(ref buffer, out var _);

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

        QueueDeclare.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}

public class QueueDeclareOkTests : Faker {
    QueueDeclareOk RandomSubject => new (
        QueueName   : Random.Word(),
        MessageCount : Random.UInt(),
        ConsumerCount: Random.UInt()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(writer);

        var buffer = writer.WrittenSpan;

        QueueDeclareOk.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.ConsumerCount, actual: deserialized?.ConsumerCount);
        Assert.Equal(expected: value.MessageCount, actual: deserialized?.MessageCount);
        Assert.Equal(expected: value.QueueName, actual: deserialized?.QueueName);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = QueueDeclareOk.Deserialize(ref buffer, out var _);

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

        QueueDeclareOk.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}
