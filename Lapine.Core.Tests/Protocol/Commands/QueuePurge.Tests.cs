namespace Lapine.Protocol.Commands;

public class QueuePurgeTests : Faker {
    QueuePurge RandomSubject => new (
        QueueName : Random.Word(),
        NoWait    : Random.Bool()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(writer);
        
        var buffer = writer.WrittenSpan;
        
        QueuePurge.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.NoWait, actual: deserialized?.NoWait);
        Assert.Equal(expected: value.QueueName, actual: deserialized?.QueueName);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = QueuePurge.Deserialize(ref buffer, out var _);

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

        QueuePurge.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}

public class QueuePurgeOkTests : Faker {
    QueuePurgeOk RandomSubject => new (
        MessageCount : Random.UInt()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(writer);
        
        var buffer = writer.WrittenSpan;
        
        QueuePurgeOk.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.MessageCount, actual: deserialized?.MessageCount);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = QueuePurgeOk.Deserialize(ref buffer, out var _);

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

        QueuePurgeOk.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}
