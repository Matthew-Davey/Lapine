namespace Lapine.Protocol.Commands;

public class TransactionCommitTests : Faker {
    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = new TransactionCommit();
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        TransactionCommit.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}

public class TransactionCommitOkTests : Faker {
    [Fact]
    public void DeserializationReturnsSurplusData() {
        var value  = new TransactionCommitOk();
        var extra  = Random.UInt();
        var buffer = new MemoryBufferWriter<Byte>();

        buffer.WriteSerializable(value)
            .WriteUInt32LE(extra);

        TransactionCommitOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

        Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
        Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
    }
}
