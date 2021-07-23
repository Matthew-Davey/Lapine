namespace Lapine.Protocol.Commands {
    using System;
    using Bogus;
    using Xunit;

    public class ChannelOpenTests : Faker {
        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = new ChannelOpen();
            var extra  = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ChannelOpen.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }

    public class ChannelOpenOkTests : Faker {
        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = new ChannelOpenOk();
            var extra  = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ChannelOpenOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
