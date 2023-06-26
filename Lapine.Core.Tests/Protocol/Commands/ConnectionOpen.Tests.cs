namespace Lapine.Protocol.Commands;

public class ConnectionOpenTests : Faker {
    ConnectionOpen RandomSubject => new (VirtualHost: Random.Word());

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(writer);
        
        var buffer = writer.WrittenSpan;
        
        ConnectionOpen.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.VirtualHost, actual: deserialized?.VirtualHost);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = ConnectionOpen.Deserialize(ref buffer, out var _);

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

        ConnectionOpen.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}

public class ConnectionOpenOkTests : Faker {
    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = new ConnectionOpenOk();
        var extra  = Random.UInt();
        var writer = new MemoryBufferWriter<Byte>();

        writer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenSpan;

        ConnectionOpenOk.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}
