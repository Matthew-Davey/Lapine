namespace Lapine.Protocol;

public class RawFrameTests : Faker {
    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>();
        var value  = new RawFrame(Random.Enum<FrameType>(), Random.UShort(), Random.Bytes(Random.UShort()));

        value.Serialize(writer);

        var buffer = writer.WrittenSpan;

        RawFrame.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.Channel, actual: deserialized?.Channel);
        Assert.Equal(expected: value.Payload.ToArray(), actual: deserialized?.Payload.ToArray());
        Assert.Equal(expected: value.Size, actual: deserialized?.Size);
        Assert.Equal(expected: value.Type, actual: deserialized?.Type);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = RawFrame.Deserialize(ref buffer, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationFailsWithInvalidFrameType() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = new RawFrame(Random.Enum<FrameType>(), Random.UShort(), Random.Bytes(Random.UShort()));

        value.Serialize(writer);
        var modifiedBuffer = writer.WrittenMemory.ToArray();
        modifiedBuffer[0] = Random.Byte(min: 10);

        Assert.Throws<FramingErrorException>(() => {
            ReadOnlySpan<Byte> buffer = modifiedBuffer.AsSpan();
            return RawFrame.Deserialize(ref buffer, out var _);
        });
    }

    [Fact]
    public void DeserializationFailsWithInvalidFrameTerminator() {
        var writer = new MemoryBufferWriter<Byte>(8);
        var value  = new RawFrame(Random.Enum<FrameType>(), Random.UShort(), Random.Bytes(Random.UShort()));

        value.Serialize(writer);
        var modifiedBuffer = writer.WrittenMemory.ToArray();
        modifiedBuffer[^1] = 0x00;

        Assert.Throws<FramingErrorException>(() => {
            ReadOnlySpan<Byte> buffer = modifiedBuffer.AsSpan();
            return RawFrame.Deserialize(ref buffer, out var _);
        });
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = new RawFrame(Random.Enum<FrameType>(), Random.UShort(), Random.Bytes(Random.UShort()));
        var extra  = Random.UInt();
        var writer = new MemoryBufferWriter<Byte>(12);

        writer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenSpan;

        RawFrame.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}
