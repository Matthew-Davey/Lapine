namespace Lapine.Protocol.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Xunit;

public class QueueDeclareTests : Faker {
    QueueDeclare RandomSubject => new (
        QueueName : Random.Word(),
        Passive   : Random.Bool(),
        Durable   : Random.Bool(),
        Exclusive : Random.Bool(),
        AutoDelete: Random.Bool(),
        NoWait    : Random.Bool(),
        Arguments : new Dictionary<String, Object> {{ Random.Word(), Random.UInt() }}
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var buffer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(buffer);
        QueueDeclare.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

        Assert.Equal(expected: value.Arguments.ToList(), actual: deserialized?.Arguments.ToList());
        Assert.Equal(expected: value.AutoDelete, actual: deserialized?.AutoDelete);
        Assert.Equal(expected: value.Durable, actual: deserialized?.Durable);
        Assert.Equal(expected: value.Exclusive, actual: deserialized?.Exclusive);
        Assert.Equal(expected: value.NoWait, actual: deserialized?.NoWait);
        Assert.Equal(expected: value.Passive, actual: deserialized?.Passive);
        Assert.Equal(expected: value.QueueName, actual: deserialized?.QueueName);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var result = QueueDeclare.Deserialize(Span<Byte>.Empty, out var _, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        QueueDeclare.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}

public class QueueDeclareOkTests : Faker {
    QueueDeclareOk RandomSubject => new (
        QueueName   : Random.Word(),
        MessageCount : Random.UInt(),
        ConsumerCount: Random.UInt()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var buffer = new MemoryBufferWriter<Byte>();
        var value  = RandomSubject;

        value.Serialize(buffer);
        QueueDeclareOk.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

        Assert.Equal(expected: value.ConsumerCount, actual: deserialized?.ConsumerCount);
        Assert.Equal(expected: value.MessageCount, actual: deserialized?.MessageCount);
        Assert.Equal(expected: value.QueueName, actual: deserialized?.QueueName);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var result = QueueDeclareOk.Deserialize(Span<Byte>.Empty, out var _, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        QueueDeclareOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}
