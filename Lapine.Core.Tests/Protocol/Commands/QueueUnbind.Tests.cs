namespace Lapine.Protocol.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Xunit;

public class QueueUnbindTests : Faker {
    QueueUnbind RandomSubject => new (
        QueueName   : Random.Word(),
        ExchangeName: Random.Word(),
        RoutingKey  : Random.Word(),
        Arguments   : new Dictionary<String, Object> {{ Random.Word(), Random.UInt() }}
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var buffer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(buffer);
        QueueUnbind.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

        Assert.Equal(expected: value.Arguments.ToList(), actual: deserialized?.Arguments.ToList());
        Assert.Equal(expected: value.ExchangeName, actual: deserialized?.ExchangeName);
        Assert.Equal(expected: value.QueueName, actual: deserialized?.QueueName);
        Assert.Equal(expected: value.RoutingKey, actual: deserialized?.RoutingKey);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var result = QueueUnbind.Deserialize(Span<Byte>.Empty, out var _, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        QueueUnbind.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}

public class QueueUnbindOkTests : Faker {
    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = new QueueUnbindOk();
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        QueueUnbindOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}
