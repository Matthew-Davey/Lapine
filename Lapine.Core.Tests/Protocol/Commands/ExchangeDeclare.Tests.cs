namespace Lapine.Protocol.Commands;

public class ExchangeDeclareTests : Faker {
    ExchangeDeclare RandomSubject => new (
        ExchangeName: Random.Word(),
        ExchangeType: Random.Word(),
        Passive     : Random.Bool(),
        Durable     : Random.Bool(),
        AutoDelete  : Random.Bool(),
        Internal    : Random.Bool(),
        NoWait      : Random.Bool(),
        Arguments   : new Dictionary<String, Object> {{ Random.Word(), Random.UInt() }}
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(writer);

        var buffer = writer.WrittenMemory;

        ExchangeDeclare.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.Arguments.ToList(), actual: deserialized?.Arguments.ToList());
        Assert.Equal(expected: value.AutoDelete, actual: deserialized?.AutoDelete);
        Assert.Equal(expected: value.Durable, actual: deserialized?.Durable);
        Assert.Equal(expected: value.ExchangeName, actual: deserialized?.ExchangeName);
        Assert.Equal(expected: value.ExchangeType, actual: deserialized?.ExchangeType);
        Assert.Equal(expected: value.Internal, actual: deserialized?.Internal);
        Assert.Equal(expected: value.NoWait, actual: deserialized?.NoWait);
        Assert.Equal(expected: value.Passive, actual: deserialized?.Passive);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlyMemory<Byte>.Empty;
        var result = ExchangeDeclare.Deserialize(ref buffer, out _);

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

        ExchangeDeclare.Deserialize(ref buffer, out _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer.Span));
    }
}

public class ExchangeDeclareOkTests : Faker {
    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = new ExchangeDeclareOk();
        var extra  = Random.UInt();
        var writer = new MemoryBufferWriter<Byte>();

        writer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenMemory;

        ExchangeDeclareOk.Deserialize(ref buffer, out _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer.Span));
    }
}
