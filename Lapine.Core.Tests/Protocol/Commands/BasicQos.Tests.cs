namespace Lapine.Protocol.Commands;

public class BasicQosTests : Faker {
    BasicQos RandomSubject => new (
        PrefetchSize : Random.UInt(),
        PrefetchCount: Random.UShort(),
        Global       : Random.Bool()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var buffer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(buffer);
        BasicQos.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

        Assert.Equal(expected: value.Global, actual: deserialized?.Global);
        Assert.Equal(expected: value.PrefetchCount, actual: deserialized?.PrefetchCount);
        Assert.Equal(expected: value.PrefetchSize, actual: deserialized?.PrefetchSize);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var result = BasicQos.Deserialize(Span<Byte>.Empty, out var _, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        BasicQos.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}

public class BasicQosOkTests : Faker {
    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = new BasicQosOk();
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        BasicQosOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}
