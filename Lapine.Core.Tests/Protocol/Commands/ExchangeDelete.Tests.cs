namespace Lapine.Protocol.Commands;

public class ExchangeDeleteTests : Faker {
    ExchangeDelete RandomSubject => new (
        ExchangeName: Random.Word(),
        IfUnused    : Random.Bool(),
        NoWait      : Random.Bool()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(writer);
        
        var buffer = writer.WrittenSpan;
        
        ExchangeDelete.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.ExchangeName, actual: deserialized?.ExchangeName);
        Assert.Equal(expected: value.IfUnused, actual: deserialized?.IfUnused);
        Assert.Equal(expected: value.NoWait, actual: deserialized?.NoWait);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = ExchangeDelete.Deserialize(ref buffer, out var _);

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

        ExchangeDelete.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}

public class ExchangeDeleteOkTests : Faker {
    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = new ExchangeDeleteOk();
        var extra  = Random.UInt();
        var writer = new MemoryBufferWriter<Byte>();

        writer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenSpan;

        ExchangeDeleteOk.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}
