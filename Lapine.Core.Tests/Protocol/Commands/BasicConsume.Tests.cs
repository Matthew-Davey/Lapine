namespace Lapine.Protocol.Commands;

public class BasicConsumeTests : Faker {
    BasicConsume RandomSubject => new (
        QueueName   : Random.Word(),
        ConsumerTag : Random.Word(),
        NoLocal     : Random.Bool(),
        NoAck       : Random.Bool(),
        Exclusive   : Random.Bool(),
        NoWait      : Random.Bool(),
        Arguments   : new Dictionary<String, Object> {{ Random.Word(), Random.UInt() }}
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(writer);

        var buffer = writer.WrittenSpan;

        BasicConsume.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.Arguments.ToList(), actual: deserialized?.Arguments.ToList());
        Assert.Equal(expected: value.ConsumerTag, actual: deserialized?.ConsumerTag);
        Assert.Equal(expected: value.Exclusive, actual: deserialized?.Exclusive);
        Assert.Equal(expected: value.NoAck, actual: deserialized?.NoAck);
        Assert.Equal(expected: value.NoLocal, actual: deserialized?.NoLocal);
        Assert.Equal(expected: value.NoWait, actual: deserialized?.NoWait);
        Assert.Equal(expected: value.QueueName, actual: deserialized?.QueueName);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = BasicConsume.Deserialize(ref buffer, out var _);

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

        BasicConsume.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}

public class BasicConsumeOkTests : Faker {
    BasicConsumeOk RandomSubject => new (
        ConsumerTag: Random.Word()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(writer);

        var buffer = writer.WrittenSpan;

        BasicConsumeOk.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.ConsumerTag, actual: deserialized?.ConsumerTag);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = BasicConsumeOk.Deserialize(ref buffer, out var _);

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

        BasicConsumeOk.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}
