namespace Lapine.Protocol {
    using System;
    using System.Buffers;
    using Xunit;

    public class ProtocolVersionTests {
        [Fact]
        public void SerializedSizeIsThreeBytes() {
            var value  = ProtocolVersion.Default;
            var buffer = new ArrayBufferWriter<Byte>(3);

            value.Serialize(buffer);

            Assert.Equal(expected: 3, actual: buffer.WrittenCount);
        }

        [Fact]
        public void DeseralizedValueIsIdentical() {
            var buffer = new ArrayBufferWriter<Byte>(3);
            ProtocolVersion.Default.Serialize(buffer);
            ProtocolVersion.Deserialize(buffer.WrittenMemory.Span, out var deserialized);

            Assert.Equal(expected: ProtocolVersion.Default, actual: deserialized);
        }
    }
}
