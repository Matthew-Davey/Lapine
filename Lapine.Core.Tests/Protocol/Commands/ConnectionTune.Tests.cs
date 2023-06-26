namespace Lapine.Protocol.Commands;

public class ConnectionTuneTests : Faker {
    ConnectionTune RandomSubject => new (ChannelMax: Random.UShort(), FrameMax: Random.UInt(), Heartbeat: Random.UShort());

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(writer);
        
        var buffer = writer.WrittenSpan;
        
        ConnectionTune.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.ChannelMax, actual: deserialized?.ChannelMax);
        Assert.Equal(expected: value.FrameMax, actual: deserialized?.FrameMax);
        Assert.Equal(expected: value.Heartbeat, actual: deserialized?.Heartbeat);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = ConnectionTune.Deserialize(ref buffer, out var _);

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

        ConnectionTune.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}

public class ConnectionTuneOkTests : Faker {
    ConnectionTuneOk RandomSubject => new (ChannelMax: Random.UShort(), FrameMax: Random.UInt(), Heartbeat: Random.UShort());

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(writer);
        
        var buffer = writer.WrittenSpan;
        
        ConnectionTuneOk.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.ChannelMax, actual: deserialized?.ChannelMax);
        Assert.Equal(expected: value.FrameMax, actual: deserialized?.FrameMax);
        Assert.Equal(expected: value.Heartbeat, actual: deserialized?.Heartbeat);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = ConnectionTuneOk.Deserialize(ref buffer, out var _);

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

        ConnectionTuneOk.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}
