namespace Lapine;

public class BufferExtensionsTests {
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
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadBits(ref buffer, out var bits);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedBits, actual: bits);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], false, new Byte[0])]
    [InlineData(true, new Byte[] { 0x01 }, true, new Byte[0])]
    [InlineData(true, new Byte[] { 0x00 }, false, new Byte[0])]
    [InlineData(true, new Byte[] { 0x01, 0xff }, true, new Byte[] { 0xff })]
    static public void ReadBoolean(in Boolean expectedResult, in Byte[] input, in Boolean expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadBoolean(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], 1U, new Byte[0], new Byte[0])]
    [InlineData(true, new Byte[] { 0x01 }, 1U, new Byte[] { 0x01 }, new Byte[0])]
    [InlineData(true, new Byte[] { 0x01, 0x02 }, 2U, new Byte[] { 0x01, 0x02 }, new Byte[0])]
    [InlineData(true, new Byte[] { 0x01, 0x02 }, 1U, new Byte[] { 0x01 }, new Byte[] { 0x02 })]
    static public void ReadBytes(in Boolean expectedResult, in Byte[] input, in UInt32 count, in Byte[] expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadBytes(ref buffer, count, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value.ToArray());
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default(Char), new Byte[0])]
    [InlineData(true, new Byte[] { 0x41 }, 'A', new Byte[0])]
    [InlineData(true, new Byte[] { 0x41, 0x42 }, 'A', new Byte[] { 0x42 })]
    static public void ReadChar(in Boolean expectedResult, in Byte[] input, in Char expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadChar(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], (UInt16)1, new Char[0], new Byte[0])]
    [InlineData(true, new Byte[] { 0x01 }, (UInt16)1, new Char[] { '\x01' }, new Byte[0])]
    [InlineData(true, new Byte[] { 0x01, 0x02 }, (UInt16)2, new Char[] { '\x01', '\x02' }, new Byte[0])]
    [InlineData(true, new Byte[] { 0x01, 0x02 }, (UInt16)1, new Char[] { '\x01' }, new Byte[] { 0x02 })]
    static public void ReadChars(in Boolean expectedResult, in Byte[] input, in UInt16 count, in Char[] expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadChars(ref buffer, count, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value.ToArray());
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default(Double), new Byte[0])]
    [InlineData(true, new Byte[] { 0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 1D, new Byte[0])]
    [InlineData(true, new Byte[] { 0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 1D, new Byte[] { 0x00 })]
    static public void ReadDouble(in Boolean expectedResult, in Byte[] input, in Double expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadDouble(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default, new Byte[0])]
    [InlineData(true, new Byte[] { (Byte)'t', 0x00 }, false, new Byte[0])]
    [InlineData(true, new Byte[] { (Byte)'t', 0x01 }, true, new Byte[0])]
    [InlineData(true, new Byte[] { (Byte)'b', 0x01 }, (SByte)0x01, new Byte[0])]
    [InlineData(true, new Byte[] { (Byte)'B', 0x01 }, (Byte)0x01, new Byte[0])]
    [InlineData(true, new Byte[] { (Byte)'s', 0x00, 0x01 }, (Int16)1, new Byte[0])]
    [InlineData(true, new Byte[] { (Byte)'u', 0x00, 0x01 }, (UInt16)1, new Byte[0])]
    [InlineData(true, new Byte[] { (Byte)'I', 0x00, 0x00, 0x00, 0x01 }, (Int32)1, new Byte[0])]
    [InlineData(true, new Byte[] { (Byte)'i', 0x00, 0x00, 0x00, 0x01 }, (UInt32)1, new Byte[0])]
    [InlineData(true, new Byte[] { (Byte)'L', 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, (Int64)1, new Byte[0])]
    [InlineData(true, new Byte[] { (Byte)'l', 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, (UInt64)1, new Byte[0])]
    [InlineData(true, new Byte[] { (Byte)'f', 0x3F, 0x80, 0x00, 0x00 }, 1f, new Byte[0])]
    [InlineData(true, new Byte[] { (Byte)'d', 0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 1d, new Byte[0])]
    [InlineData(true, new Byte[] { (Byte)'S', 0x00, 0x00, 0x00, 0x04, 0x74, 0x65, 0x73, 0x74 }, "test", new Byte[0])]
    // TODO: decimal, field-array, timestamp, no-field, field-table
    static public void ReadFieldValue(in Boolean expectedResult, Byte[] input, Object expectedValue, Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadFieldValue(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default(SByte), new Byte[0])]
    [InlineData(true, new Byte[] { 0x01 }, (SByte)1, new Byte[0])]
    [InlineData(true, new Byte[] { 0x01, 0x00 }, (SByte)1, new Byte[] { 0x00 })]
    static public void ReadInt8(in Boolean expectedResult, in Byte[] input, in SByte expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadInt8(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default(Int16), new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x01 }, (Int16)1, new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x01, 0x00 }, (Int16)1, new Byte[] { 0x00 })]
    static public void ReadInt16BE(in Boolean expectedResult, in Byte[] input, in Int16 expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadInt16BE(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default(Int32), new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x00, 0x00, 0x01 }, (Int32)1, new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x00, 0x00, 0x01, 0x00 }, (Int32)1, new Byte[] { 0x00 })]
    static public void ReadInt32BE(in Boolean expectedResult, in Byte[] input, in Int32 expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadInt32BE(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default(Int64), new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, (Int64)1, new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00 }, (Int64)1, new Byte[] { 0x00 })]
    static public void ReadInt64BE(in Boolean expectedResult, in Byte[] input, in Int64 expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadInt64BE(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default(String), new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x00, 0x00, 0x04, 0x74, 0x65, 0x73, 0x74 }, "test", new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x00, 0x00, 0x04, 0x74, 0x65, 0x73, 0x74, 0x00 }, "test", new Byte[] { 0x00 })]
    static public void ReadLongString(in Boolean expectedResult, in Byte[] input, String expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadLongString(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default(UInt16), default(UInt16), new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x01, 0x00, 0x01 }, (UInt16)1, (UInt16)1, new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x01, 0x00, 0x01, 0x00 }, (UInt16)1, (UInt16)1, new Byte[] { 0x00 })]
    static public void ReadMethodHeader(in Boolean expectedResult, in Byte[] input, in UInt16 expectedClassId, in UInt16 expectedMethodId, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadMethodHeader(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedClassId, actual: value.ClassId);
        Assert.Equal(expected: expectedMethodId, actual: value.MethodId);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default(String), new Byte[0])]
    [InlineData(true, new Byte[] { 0x04, 0x74, 0x65, 0x73, 0x74 }, "test", new Byte[0])]
    [InlineData(true, new Byte[] { 0x04, 0x74, 0x65, 0x73, 0x74, 0x00 }, "test", new Byte[] { 0x00 })]
    static public void ReadShortString(in Boolean expectedResult, in Byte[] input, String expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadShortString(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default(Single), new Byte[0])]
    [InlineData(true, new Byte[] { 0x3F, 0x80, 0x00, 0x00 }, (Single)1, new Byte[0])]
    [InlineData(true, new Byte[] { 0x3F, 0x80, 0x00, 0x00, 0x00 }, (Single)1, new Byte[] { 0x00 })]
    static public void ReadSingle(in Boolean expectedResult, in Byte[] input, in Single expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadSingle(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default(Byte), new Byte[0])]
    [InlineData(true, new Byte[] { 0x01 }, (Byte)1, new Byte[0])]
    [InlineData(true, new Byte[] { 0x01, 0x00 }, (Byte)1, new Byte[] { 0x00 })]
    static public void ReadUInt8(in Boolean expectedResult, in Byte[] input, in Byte expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadUInt8(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default(UInt16), new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x01 }, (UInt16)1, new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x01, 0x00 }, (UInt16)1, new Byte[] { 0x00 })]
    static public void ReadUInt16BE(in Boolean expectedResult, in Byte[] input, in UInt16 expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadUInt16BE(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default(UInt32), new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x00, 0x00, 0x01 }, (UInt32)1, new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x00, 0x00, 0x01, 0x00 }, (UInt32)1, new Byte[] { 0x00 })]
    static public void ReadUInt32BE(in Boolean expectedResult, in Byte[] input, in UInt32 expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadUInt32BE(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }

    [Theory]
    [InlineData(false, new Byte[0], default(UInt64), new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, (UInt64)1, new Byte[0])]
    [InlineData(true, new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00 }, (UInt64)1, new Byte[] { 0x00 })]
    static public void ReadUInt64BE(in Boolean expectedResult, in Byte[] input, in UInt64 expectedValue, in Byte[] expectedSurplus) {
        var buffer = new ReadOnlySpan<Byte>(input);
        var result = BufferExtensions.ReadUInt64BE(ref buffer, out var value);

        Assert.Equal(expected: expectedResult, actual: result);
        Assert.Equal(expected: expectedValue, actual: value);
        Assert.Equal(expected: expectedSurplus, actual: buffer.ToArray());
    }
}
