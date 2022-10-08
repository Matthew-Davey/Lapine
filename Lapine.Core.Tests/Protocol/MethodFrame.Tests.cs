namespace Lapine.Protocol;

using Lapine.Protocol.Commands;

public class MethodFrameTests : Faker {
    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>();
        var method = new ConnectionSecure(Random.String2(8));
        var value  = new MethodFrame(Random.UShort(), method.CommandId, method);

        value.Serialize(writer);
        var buffer = writer.WrittenMemory;
        var deserializationSucceeded = Frame.Deserialize(ref buffer, out var deserialized);

        Assert.True(deserializationSucceeded);

        if (deserializationSucceeded) {
            Assert.IsType<MethodFrame>(deserialized);

            var (channel, methodHeader, command) = (MethodFrame) deserialized;

            Assert.Equal(expected: FrameType.Method, actual: deserialized.Type);
            Assert.Equal(expected: value.Channel, actual: channel);
            Assert.Equal(expected: value.MethodHeader, actual: methodHeader);
            Assert.Equal(expected: value.Command, actual: command);
        }
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlyMemory<Byte>.Empty;
        var result = MethodFrame.Deserialize(ref buffer, out _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationFailsWithInvalidFrameType() {
        var writer = new MemoryBufferWriter<Byte>();
        var method = new ConnectionSecure(Random.String2(8));
        var value  = new MethodFrame(Random.UShort(), method.CommandId, method);

        value.Serialize(writer);
        var modifiedBuffer = writer.WrittenMemory.ToArray();
        modifiedBuffer[0] = Random.Byte(min: 10);
        var buffer = new ReadOnlyMemory<Byte>(modifiedBuffer);

        Assert.Throws<FramingErrorException>(() => MethodFrame.Deserialize(ref buffer, out _));
    }

    [Fact]
    public void DeserializationFailsWithInvalidFrameTerminator() {
        var writer = new MemoryBufferWriter<Byte>();
        var method = new ConnectionSecure(Random.String2(8));
        var value  = new MethodFrame(Random.UShort(), method.CommandId, method);

        value.Serialize(writer);
        var modifiedBuffer = writer.WrittenMemory.ToArray();
        modifiedBuffer[^1] = 0x00;
        var buffer = new ReadOnlyMemory<Byte>(modifiedBuffer);

        Assert.Throws<FramingErrorException>(() => MethodFrame.Deserialize(ref buffer, out var _));
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var method = new ConnectionSecure(Random.String2(8));
        var value  = new MethodFrame(Random.UShort(), method.CommandId, method);
        var extra  = Random.UInt();
        var writer = new MemoryBufferWriter<Byte>(12);

        writer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenMemory;

        MethodFrame.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer.Span));
    }
}
