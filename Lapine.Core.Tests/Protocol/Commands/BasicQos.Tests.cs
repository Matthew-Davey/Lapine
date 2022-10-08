namespace Lapine.Protocol.Commands;

public class BasicQosTests : Faker {
    BasicQos RandomSubject => new (
        PrefetchSize : Random.UInt(),
        PrefetchCount: Random.UShort(),
        Global       : Random.Bool()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(writer);

        var buffer = writer.WrittenMemory;

        BasicQos.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.Global, actual: deserialized?.Global);
        Assert.Equal(expected: value.PrefetchCount, actual: deserialized?.PrefetchCount);
        Assert.Equal(expected: value.PrefetchSize, actual: deserialized?.PrefetchSize);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlyMemory<Byte>.Empty;
        var result = BasicQos.Deserialize(ref buffer, out _);

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

        BasicQos.Deserialize(ref buffer, out _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer.Span));
    }
}

public class BasicQosOkTests : Faker {
    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = new BasicQosOk();
        var extra  = Random.UInt();
        var writer = new MemoryBufferWriter<Byte>();

        writer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenMemory;

        BasicQosOk.Deserialize(ref buffer, out _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer.Span));
    }
}
