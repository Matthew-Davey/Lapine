namespace Lapine.Protocol.Commands;

public class ConnectionCloseTests : Faker {
    ConnectionClose RandomSubject => new (
        ReplyCode    : Random.UShort(),
        ReplyText    : Lorem.Sentence(wordCount: Random.Int(min: 1, max: 16)),
        FailingMethod: (Random.UShort(), Random.UShort())
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var writer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(writer);

        var buffer = writer.WrittenSpan;

        ConnectionClose.Deserialize(ref buffer, out var deserialized);

        Assert.Equal(expected: value.FailingMethod, actual: deserialized?.FailingMethod);
        Assert.Equal(expected: value.ReplyCode, actual: deserialized?.ReplyCode);
        Assert.Equal(expected: value.ReplyText, actual: deserialized?.ReplyText);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var buffer = ReadOnlySpan<Byte>.Empty;
        var result = ConnectionClose.Deserialize(ref buffer, out var _);

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

        ConnectionClose.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}

public class ConnectionCloseOkTests : Faker {
    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = new ConnectionCloseOk();
        var extra  = Random.UInt();
        var writer = new MemoryBufferWriter<Byte>();

        writer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        var buffer = writer.WrittenSpan;

        ConnectionCloseOk.Deserialize(ref buffer, out var _);

        Assert.Equal(expected: sizeof(UInt32), actual: buffer.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(buffer));
    }
}
