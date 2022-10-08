namespace Lapine.Protocol.Commands;

public class ChannelFlowTests : Faker {
    ChannelFlow RandomSubject => new (
        Active: Random.Bool()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(writer);

        var buffer = writer.WrittenMemory;

        ChannelFlow.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.Active, actual: deserialized?.Active);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlyMemory<Byte>.Empty;
        var result = ChannelFlow.Deserialize(ref buffer, out _);

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

        ChannelFlow.Deserialize(ref buffer, out _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer.Span));
    }
}

public class ChannelFlowOkTests : Faker {
    ChannelFlowOk RandomSubject => new (
        Active: Random.Bool()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(writer);

        var buffer = writer.WrittenMemory;

        ChannelFlowOk.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.Active, actual: deserialized?.Active);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlyMemory<Byte>.Empty;
        var result = ChannelFlowOk.Deserialize(ref buffer, out _);

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

        ChannelFlowOk.Deserialize(ref buffer, out _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer.Span));
    }
}
