namespace Lapine.Protocol {
    using System;
    using System.Buffers;
    using Xunit;

    public class FrameHeaderTests {
        [Fact]
        public void SerializedSizeIsSevenBytes() {
            var value  = new FrameHeader(FrameType.Method, 1, 1024);
            var buffer = new ArrayBufferWriter<Byte>(8);

            value.Serialize(buffer);

            Assert.Equal(expected: 7, actual: buffer.WrittenCount);
        }

        [Fact]
        public void DeseralizedValueIsIdentical() {
            var value = new FrameHeader(FrameType.Method, 1, 1024);
            var buffer = new ArrayBufferWriter<Byte>(8);
            value.Serialize(buffer);
            FrameHeader.Deserialize(buffer.WrittenMemory.Span, out var deserialized);

            Assert.Equal(expected: value, actual: deserialized);
        }
    }
}
