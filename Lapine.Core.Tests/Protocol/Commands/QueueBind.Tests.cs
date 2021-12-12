namespace Lapine.Protocol.Commands;

public class QueueBindTests : Faker {
    QueueBind RandomSubject => new (
        QueueName   : Random.Word(),
        ExchangeName: Random.Word(),
        RoutingKey  : Random.Word(),
        NoWait      : Random.Bool(),
        Arguments   : new Dictionary<String, Object> {{ Random.Word(), Random.UInt() }}
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var buffer = new MemoryBufferWriter<Byte>(10);
        var value  = RandomSubject;

        value.Serialize(buffer);
        QueueBind.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

        Assert.Equal(expected: value.Arguments.ToList(), actual: deserialized?.Arguments.ToList());
        Assert.Equal(expected: value.ExchangeName, actual: deserialized?.ExchangeName);
        Assert.Equal(expected: value.NoWait, actual: deserialized?.NoWait);
        Assert.Equal(expected: value.QueueName, actual: deserialized?.QueueName);
        Assert.Equal(expected: value.RoutingKey, actual: deserialized?.RoutingKey);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var result = QueueBind.Deserialize(Span<Byte>.Empty, out var _, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        QueueBind.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}

public class QueueBindOkTests : Faker {
    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = new QueueBindOk();
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        QueueBindOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}
