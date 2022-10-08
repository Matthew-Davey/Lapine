namespace Lapine.Protocol;

public class ProtocolHeaderTests : Faker {
    [Fact]
    public void SerializedSizeIsEightBytes() {
        var value  = ProtocolHeader.Create(Random.Chars(count: 4), Random.Byte(), new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte()));
        var writer = new MemoryBufferWriter<Byte>(8);

        value.Serialize(writer);

        Assert.Equal(expected: 8, actual: writer.WrittenCount);
    }

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = ProtocolHeader.Create(Random.Chars(count: 4), Random.Byte(), new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte()));

        value.Serialize(writer);
        var buffer = writer.WrittenMemory;

        ProtocolHeader.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value, actual: deserialized);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlyMemory<Byte>.Empty;
        var result = ProtocolHeader.Deserialize(ref buffer, out _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = ProtocolHeader.Create(Random.Chars(count: 4), Random.Byte(), new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte()));
        var extra  = Random.UInt();
        var writer = new MemoryBufferWriter<Byte>(12);

        writer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenMemory;
        ProtocolHeader.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer.Span));
    }
}
