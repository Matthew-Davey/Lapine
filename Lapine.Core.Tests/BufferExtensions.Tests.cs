namespace Lapine {
    using System;
    using Xunit;

    public class BufferExtensionsTests
    {
        [Theory]
        [InlineData(false, new Byte[0], null, new Byte[0])]
        [InlineData(true, new Byte[] { 0x00 }, new Boolean[] { false, false, false, false, false, false, false, false }, new Byte[0])]
        [InlineData(true, new Byte[] { 0x01 }, new Boolean[] { true, false, false, false, false, false, false, false }, new Byte[0])]
        [InlineData(true, new Byte[] { 0x02 }, new Boolean[] { false, true, false, false, false, false, false, false }, new Byte[0])]
        [InlineData(true, new Byte[] { 0x03 }, new Boolean[] { true, true, false, false, false, false, false, false }, new Byte[0])]
        [InlineData(true, new Byte[] { 0x04 }, new Boolean[] { false, false, true, false, false, false, false, false }, new Byte[0])]
        [InlineData(true, new Byte[] { 0x05 }, new Boolean[] { true, false, true, false, false, false, false, false }, new Byte[0])]
        [InlineData(true, new Byte[] { 0x06 }, new Boolean[] { false, true, true, false, false, false, false, false }, new Byte[0])]
        [InlineData(true, new Byte[] { 0x07 }, new Boolean[] { true, true, true, false, false, false, false, false }, new Byte[0])]
        [InlineData(true, new Byte[] { 0x01, 0xff }, new Boolean[] { true, false, false, false, false, false, false, false }, new Byte[] { 0xff })]
        static public void ReadBits(in Boolean expectedResult, in Byte[] input, in Boolean[] expectedBits, in Byte[] expectedSurplus) {
            var result = BufferExtensions.ReadBits(new ReadOnlySpan<Byte>(input), out var bits, out var surplus);

            Assert.Equal(expected: expectedResult, actual: result);
            Assert.Equal(expected: expectedBits, actual: bits);
            Assert.Equal(expected: expectedSurplus, actual: surplus.ToArray());
        }

        [Theory]
        [InlineData(false, new Byte[0], false, new Byte[0])]
        [InlineData(true, new Byte[] { 0x01 }, true, new Byte[0])]
        [InlineData(true, new Byte[] { 0x00 }, false, new Byte[0])]
        [InlineData(true, new Byte[] { 0x01, 0xff }, true, new Byte[] { 0xff })]
        static public void ReadBoolean(in Boolean expectedResult, in Byte[] input, in Boolean expectedValue, in Byte[] expectedSurplus) {
            var result = BufferExtensions.ReadBoolean(new ReadOnlySpan<Byte>(input), out var value, out var surplus);

            Assert.Equal(expected: expectedResult, actual: result);
            Assert.Equal(expected: expectedValue, actual: value);
            Assert.Equal(expected: expectedSurplus, actual: surplus.ToArray());
        }

        [Theory]
        [InlineData(false, new Byte[0], 1U, new Byte[0], new Byte[0])]
        [InlineData(true, new Byte[] { 0x01 }, 1U, new Byte[] { 0x01 }, new Byte[0])]
        [InlineData(true, new Byte[] { 0x01, 0x02 }, 2U, new Byte[] { 0x01, 0x02 }, new Byte[0])]
        [InlineData(true, new Byte[] { 0x01, 0x02 }, 1U, new Byte[] { 0x01 }, new Byte[] { 0x02 })]
        static public void ReadBytes(in Boolean expectedResult, in Byte[] input, in UInt32 count, in Byte[] expectedValue, in Byte[] expectedSurplus) {
            var result = BufferExtensions.ReadBytes(input, count, out var value, out var surplus);

            Assert.Equal(expected: expectedResult, actual: result);
            Assert.Equal(expected: expectedValue, actual: value.ToArray());
            Assert.Equal(expected: expectedSurplus, actual: surplus.ToArray());
        }

        [Theory]
        [InlineData(false, new Byte[0], default(Char), new Byte[0])]
        [InlineData(true, new Byte[] { 0x41 }, 'A', new Byte[0])]
        [InlineData(true, new Byte[] { 0x41, 0x42 }, 'A', new Byte[] { 0x42 })]
        static public void ReadChar(in Boolean expectedResult, in Byte[] input, in Char expectedValue, in Byte[] expectedSurplus) {
            var result = BufferExtensions.ReadChar(input, out var value, out var surplus);

            Assert.Equal(expected: expectedResult, actual: result);
            Assert.Equal(expected: expectedValue, actual: value);
            Assert.Equal(expected: expectedSurplus, actual: surplus.ToArray());
        }

        [Theory]
        [InlineData(false, new Byte[0], (UInt16)1, new Char[0], new Byte[0])]
        [InlineData(true, new Byte[] { 0x01 }, (UInt16)1, new Char[] { '\x01' }, new Byte[0])]
        [InlineData(true, new Byte[] { 0x01, 0x02 }, (UInt16)2, new Char[] { '\x01', '\x02' }, new Byte[0])]
        [InlineData(true, new Byte[] { 0x01, 0x02 }, (UInt16)1, new Char[] { '\x01' }, new Byte[] { 0x02 })]
        static public void ReadChars(in Boolean expectedResult, in Byte[] input, in UInt16 count, in Char[] expectedValue, in Byte[] expectedSurplus) {
            var result = BufferExtensions.ReadChars(input, count, out var value, out var surplus);

            Assert.Equal(expected: expectedResult, actual: result);
            Assert.Equal(expected: expectedValue, actual: value.ToArray());
            Assert.Equal(expected: expectedSurplus, actual: surplus.ToArray());
        }
    }
}
