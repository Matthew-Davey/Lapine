namespace Lapine.Protocol {
    using System;
    using System.Buffers;
    using Xunit;

    public class ProtocolHeaderTests {
        [Fact]
        public void SerializedSizeIsEightBytes() {
            var value  = ProtocolHeader.Default;
            var buffer = new ArrayBufferWriter<Byte>(8);

            value.Serialize(buffer);

            Assert.Equal(expected: 8, actual: buffer.WrittenCount);
        }

        [Fact]
        public void DeseralizedValueIsIdentical() {
            var buffer = new ArrayBufferWriter<Byte>(8);
            ProtocolHeader.Default.Serialize(buffer);
            ProtocolHeader.Deserialize(buffer.WrittenMemory.Span, out var deserialized);

            Assert.Equal(expected: ProtocolHeader.Default, actual: deserialized);
        }
    }
}
