namespace Lapine.Protocol;

using Lapine.Protocol.Commands;

public class MethodFrameTests : Faker {
    [Fact]
    public void SerializationIsSymmetric() {
        var buffer = new MemoryBufferWriter<Byte>();
        var method = new ConnectionSecure(Random.String2(8));
        var value  = new MethodFrame(Random.UShort(), method.CommandId, method);

        value.Serialize(buffer);
        var deserializationSucceeded = Frame.Deserialize(buffer.WrittenSpan, out var deserialized, out var _);

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
        var result = MethodFrame.Deserialize(Span<Byte>.Empty, out var _, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationFailsWithInvalidFrameType() {
        var buffer = new MemoryBufferWriter<Byte>();
        var method = new ConnectionSecure(Random.String2(8));
        var value  = new MethodFrame(Random.UShort(), method.CommandId, method);

        value.Serialize(buffer);
        var modifiedBuffer = buffer.WrittenMemory.ToArray();
        modifiedBuffer[0] = Random.Byte(min: 10);

        Assert.Throws<FramingErrorException>(() => MethodFrame.Deserialize(modifiedBuffer.AsSpan(), out var _, out var _));
    }

    [Fact]
    public void DeserializationFailsWithInvalidFrameTerminator() {
        var buffer = new MemoryBufferWriter<Byte>();
        var method = new ConnectionSecure(Random.String2(8));
        var value  = new MethodFrame(Random.UShort(), method.CommandId, method);

        value.Serialize(buffer);
        var modifiedBuffer = buffer.WrittenMemory.ToArray();
        modifiedBuffer[^1] = 0x00;

        Assert.Throws<FramingErrorException>(() => MethodFrame.Deserialize(modifiedBuffer.AsSpan(), out var _, out var _));
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var method = new ConnectionSecure(Random.String2(8));
        var value  = new MethodFrame(Random.UShort(), method.CommandId, method);
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>(12);

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        MethodFrame.Deserialize(buffer.WrittenSpan, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}
