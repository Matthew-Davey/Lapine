namespace Lapine.Protocol.Commands;

using System;
using Bogus;
using Xunit;

public class ConfirmSelectTests : Faker {
    ConfirmSelect RandomSubject => new (
        NoWait: Random.Bool()
    );

    [Fact]
    public void SerializationIsSymmetric() {
        var buffer = new MemoryBufferWriter<Byte>(8);
        var value  = RandomSubject;

        value.Serialize(buffer);
        ConfirmSelect.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

        Assert.Equal(expected: value.NoWait, actual: deserialized?.NoWait);
    }

    [Fact]
    public void DeserializationFailsWithInsufficientData() {
        var result = ConfirmSelect.Deserialize(Span<Byte>.Empty, out var _, out var _);

        Assert.False(result);
    }

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        ConfirmSelect.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}

public class ConfirmSelectOkTests : Faker {
    ConfirmSelectOk RandomSubject => new ();

    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = RandomSubject;
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        ConfirmSelectOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}
