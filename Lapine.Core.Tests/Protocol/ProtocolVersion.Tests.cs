namespace Lapine.Protocol;

public class ProtocolVersionTests : Faker {
    [Fact]
    public void SerializedSizeIsThreeBytes() {
        var value  = new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte());
        var writer = new MemoryBufferWriter<Byte>(3);

        value.Serialize(writer);

        Assert.Equal(expected: 3, actual: writer.WrittenCount);
    }

    [Fact]
    public void SerializationIsSymmetric() {
        var value  = new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte());
        var writer = new MemoryBufferWriter<Byte>(3);

        value.Serialize(writer);
        var buffer = writer.WrittenMemory;

        ProtocolVersion.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value, actual: deserialized);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlyMemory<Byte>.Empty;
        var result = ProtocolVersion.Deserialize(ref buffer, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = new ProtocolVersion(Random.Byte(), Random.Byte(), Random.Byte());
        var extra  = Random.UInt();
        var writer = new MemoryBufferWriter<Byte>(7);

        writer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenMemory;
        ProtocolVersion.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer.Span));
    }
}
