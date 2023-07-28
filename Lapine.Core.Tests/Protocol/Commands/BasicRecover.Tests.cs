namespace Lapine.Protocol.Commands;

public class BasicRecoverTests : Faker {
    BasicRecover RandomSubject => new (
        ReQueue: Random.Bool()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(writer);

        var buffer = writer.WrittenSpan;

        BasicRecover.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.ReQueue, actual: deserialized?.ReQueue);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = BasicRecover.Deserialize(ref buffer, out var _);

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

        BasicRecover.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}

public class BasicRecoverOkTests : Faker {
    [Fact]
    public void DeserializationReturnsSurplusData() {
        var extra  = Random.UInt();
        var writer = new MemoryBufferWriter<Byte>();

        writer.WriteSerializable(new BasicRecoverOk())
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenSpan;

        BasicRecoverOk.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}
