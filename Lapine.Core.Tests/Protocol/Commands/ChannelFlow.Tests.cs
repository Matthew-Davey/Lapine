namespace Lapine.Protocol.Commands;

public class ChannelFlowTests : Faker {
    ChannelFlow RandomSubject => new (
        Active: Random.Bool()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var buffer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(buffer);
        ChannelFlow.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

        Assert.Equal(expected: value.Active, actual: deserialized?.Active);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var result = ChannelFlow.Deserialize(Span<Byte>.Empty, out var _, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        ChannelFlow.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}

public class ChannelFlowOkTests : Faker {
    ChannelFlowOk RandomSubject => new (
        Active: Random.Bool()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var buffer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(buffer);
        ChannelFlowOk.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

        Assert.Equal(expected: value.Active, actual: deserialized?.Active);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var result = ChannelFlowOk.Deserialize(Span<Byte>.Empty, out var _, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        ChannelFlowOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}
