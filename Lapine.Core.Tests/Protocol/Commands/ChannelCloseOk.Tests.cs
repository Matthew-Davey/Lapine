namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using Bogus;
    using Xunit;

    public class ChannelCloseOkTests : Faker {
        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = new ChannelCloseOk();
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ChannelCloseOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
