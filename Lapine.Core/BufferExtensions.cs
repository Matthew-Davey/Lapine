namespace Lapine;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

using static System.Buffers.Binary.BinaryPrimitives;
using static System.Text.Encoding;

// ref: https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/e00b71045d3163e057f2b857cb881872413ff03b/projects/RabbitMQ.Client/client/impl/WireFormatting.Read.cs
// ref: https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/e00b71045d3163e057f2b857cb881872413ff03b/projects/RabbitMQ.Client/client/impl/WireFormatting.Write.cs
static class BufferExtensions {
    static public Boolean ReadBits(ref this ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out Boolean[]? result) {
        if (buffer.Length < 1) {
            result = default;
            return false;
        }

        result = new Boolean[8];

        var bits = buffer[0];
        var mask = 0x01;

        for (var i = 0; i < 8; i++) {
            result[i] = (bits & mask) != 0;
            mask <<= 1;
        }

        buffer = buffer[1..];
        return true;
    }

    static public Boolean ReadBoolean(ref this ReadOnlySpan<Byte> buffer, out Boolean result) {
        if (buffer.Length < 1) {
            result  = default;
            return false;
        }

        result  = buffer[0] > 0;
        buffer = buffer[1..];
        return true;
    }

    static public Boolean ReadBytes(ref this ReadOnlySpan<Byte> buffer, in UInt32 number, out ReadOnlySpan<Byte> result) {
        if (buffer.Length < number) {
            result  = default;
            return false;
        }

        result  = buffer[0..(Int32)number];
        buffer = buffer[(Int32)number..];
        return true;
    }

    static public Boolean ReadChar(ref this ReadOnlySpan<Byte> buffer, out Char result) {
        if (buffer.Length < 1) {
            result  = default;
            return false;
        }

        result  = (Char)buffer[0];
        buffer = buffer[1..];
        return true;
    }

    static public Boolean ReadChars(ref this ReadOnlySpan<Byte> buffer, in UInt16 number, out ReadOnlySpan<Char> result) {
        if (buffer.Length < number) {
            result  = default;
            return false;
        }

        result  = ASCII.GetString(buffer[0..number]).AsSpan();
        buffer = buffer[number..];
        return true;
    }

    static public Boolean ReadDouble(ref this ReadOnlySpan<Byte> buffer, out Double result) {
        if (buffer.Length < sizeof(Double)) {
            result = default;
            return false;
        }

        result = ReadDoubleBigEndian(buffer);
        buffer = buffer[sizeof(Double)..];
        return true;
    }

    static public Boolean ReadFieldArray(ref this ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out IList<Object>? result) {
        if (buffer.ReadInt32BE(out var arrayLength) &&
            buffer.ReadBytes((UInt32)arrayLength, out var arrayBytes))
        {
            var fieldArray = new List<Object>();

            while (arrayBytes.Length > 0) {
                if (arrayBytes.ReadFieldValue(out var fieldValue)) {
                    fieldArray.Add(fieldValue);
                    continue;
                }
                else {
                    result = default;
                    return false;
                }
            }

            result = fieldArray;
            return true;
        }
        else {
            result = default;
            return false;
        }
    }

    static public Boolean ReadFieldTable(ref this ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out IReadOnlyDictionary<String, Object>? result) {
        if (buffer.ReadUInt32BE(out var tableLength) &&
            buffer.ReadBytes(tableLength, out var tableBytes))
        {
            var fields = new Dictionary<String, Object>();

            while (tableBytes.Length > 0) {
                if (tableBytes.ReadShortString(out var fieldName) &&
                    tableBytes.ReadFieldValue(out var fieldValue))
                {
                    fields.Add(fieldName, fieldValue);
                    continue;
                }
                else {
                    result = default;
                    return false;
                }
            }

            result = fields;
            return true;
        }
        else {
            result = default;
            return false;
        }
    }

    static public Boolean ReadFieldValue(ref this ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out Object? result) {
        if (buffer.ReadChar(out var fieldType) == false) {
            result = default;
            return false;
        }

        switch (fieldType) {
            case 't': { // boolean
                if (buffer.ReadBoolean(out var value)) {
                    result = value;
                    return true;
                }
                break;
            }
            case 'b': { // short-short-int
                if (buffer.ReadInt8(out var value)) {
                    result = value;
                    return true;
                }
                break;
            }
            case 'B': { // short-short-uint
                if (buffer.ReadUInt8(out var value)) {
                    result = value;
                    return true;
                }
                break;
            }
            case 's': { // short-int
                if (buffer.ReadInt16BE(out var value)) {
                    result = value;
                    return true;
                }
                break;
            }
            case 'u': { // short-uint
                if (buffer.ReadUInt16BE(out var value)) {
                    result = value;
                    return true;
                }
                break;
            }
            case 'I': { // long-int
                if (buffer.ReadInt32BE(out var value)) {
                    result = value;
                    return true;
                }
                break;
            }
            case 'i': { // long-uint
                if (buffer.ReadUInt32BE(out var value)) {
                    result = value;
                    return true;
                }
                break;
            }
            case 'L': { // long-long-int
                if (buffer.ReadInt64BE(out var value)) {
                    result = value;
                    return true;
                }
                break;
            }
            case 'l': { // long-long-uint
                if (buffer.ReadUInt64BE(out var value)) {
                    result = value;
                    return true;
                }
                break;
            }
            case 'f': { // float
                if (buffer.ReadSingle(out var value)) {
                    result = value;
                    return true;
                }
                break;
            }
            case 'd': { // double
                if (buffer.ReadDouble(out var value)) {
                    result = value;
                    return true;
                }
                break;
            }
            case 'D': { // decimal-value
                result = default(Decimal); // TODO: read and decode decimal-value (see: https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/e00b71045d3163e057f2b857cb881872413ff03b/projects/RabbitMQ.Client/client/impl/WireFormatting.Read.cs#L45)
                buffer = buffer[5..];
                return true;
            }
            case 'S': { // long-string
                if (buffer.ReadLongString(out var value)) {
                    result = value;
                    return true;
                }
                break;
            }
            case 'x': { // byte-array
                if (buffer.ReadUInt32BE(out var length) &&
                    buffer.ReadBytes(length, out var value)) {
                        result = value.ToArray();
                        return true;
                }
                break;
            }
            case 'A' : { // field-array
                if (buffer.ReadFieldArray(out var value)) {
                    result = value;
                    return true;
                }
                break;
            }
            case 'T': { // timestamp
                if (buffer.ReadUInt64BE(out var value)) {
                    result = DateTimeOffset.FromUnixTimeSeconds((Int64)value);
                    return true;
                }
                break;
            }
            case 'V': { // no-field
                result = DBNull.Value;
                return true;
            }
            case 'F': { // field-table
                if (buffer.ReadFieldTable(out var value)) {
                    result = value;
                    return true;
                }
                break;
            }
        }

        result = default;
        return false;
    }

    static public Boolean ReadInt8(ref this ReadOnlySpan<Byte> buffer, out SByte result) {
        if (buffer.Length < 1) {
            result  = default;
            return false;
        }

        result = (SByte)buffer[0];
        buffer = buffer[1..];
        return true;
    }

    static public Boolean ReadInt16BE(ref this ReadOnlySpan<Byte> buffer, out Int16 result) {
        if (buffer.Length < sizeof(Int16)) {
            result  = default;
            return false;
        }

        result  = ReadInt16BigEndian(buffer);
        buffer = buffer[sizeof(Int16)..];
        return true;
    }

    static public Boolean ReadInt32BE(ref this ReadOnlySpan<Byte> buffer, out Int32 result) {
        if (buffer.Length < sizeof(Int32)) {
            result  = default;
            return false;
        }

        result  = ReadInt32BigEndian(buffer);
        buffer = buffer[sizeof(Int32)..];
        return true;
    }

    static public Boolean ReadInt64BE(ref this ReadOnlySpan<Byte> buffer, out Int64 result) {
        if (buffer.Length < sizeof(Int64)) {
            result  = default;
            return false;
        }

        result  = ReadInt64BigEndian(buffer);
        buffer = buffer[sizeof(Int64)..];
        return true;
    }

    static public Boolean ReadLongString(ref this ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out String? result) {
        if (ReadUInt32BE(ref buffer, out var length) &&
            ReadBytes(ref buffer, length, out var bytes))
        {
            result = UTF8.GetString(bytes);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }

    static public Boolean ReadMethodHeader(ref this ReadOnlySpan<Byte> buffer, out (UInt16 ClassId, UInt16 MethodId) result) {
        if (ReadUInt16BE(ref buffer, out var classId) &&
            ReadUInt16BE(ref buffer, out var methodId))
        {
            result = (classId, methodId);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }

    static public Boolean ReadShortString(ref this ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out String? result) {
        if (ReadUInt8(ref buffer, out var length) &&
            ReadBytes(ref buffer, length, out var bytes))
        {
            result = UTF8.GetString(bytes);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }

    static public Boolean ReadSingle(ref this ReadOnlySpan<Byte> buffer, out Single result) {
        if (buffer.Length < sizeof(Single)) {
            result = default;
            return false;
        }

        result = ReadSingleBigEndian(buffer);
        buffer = buffer[sizeof(Single)..];
        return true;
    }

    static public Boolean ReadUInt8(ref this ReadOnlySpan<Byte> buffer, out Byte result) {
        if (buffer.Length < sizeof(Byte)) {
            result  = default;
            return false;
        }

        result  = buffer[0];
        buffer = buffer[sizeof(Byte)..];
        return true;
    }

    static public Boolean ReadUInt16BE(ref this ReadOnlySpan<Byte> buffer, out UInt16 result) {
        if (buffer.Length < sizeof(UInt16)) {
            result  = default;
            return false;
        }

        result  = ReadUInt16BigEndian(buffer);
        buffer = buffer[sizeof(UInt16)..];
        return true;
    }

    static public Boolean ReadUInt32BE(ref this ReadOnlySpan<Byte> buffer, out UInt32 result) {
        if (buffer.Length < sizeof(UInt32)) {
            result  = default;
            return false;
        }

        result  = ReadUInt32BigEndian(buffer);
        buffer = buffer[sizeof(UInt32)..];
        return true;
    }

    static public Boolean ReadUInt64BE(ref this ReadOnlySpan<Byte> buffer, out UInt64 result) {
        if (buffer.Length < sizeof(UInt64)) {
            result  = default;
            return false;
        }

        result  = ReadUInt64BigEndian(buffer);
        buffer = buffer[sizeof(UInt64)..];
        return true;
    }

    static public IBufferWriter<Byte> WriteBits(this IBufferWriter<Byte> writer, params Boolean[] values) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        if (values is null)
            throw new ArgumentNullException(nameof(values));

        if (values.Length < 2)
            throw new ArgumentException("Must have at least 2 bits to pack");

        if (values.Length > 8)
            throw new ArgumentException("Cannot pack more than 8 bits");

        var result = (Byte)0;
        var mask = (Byte)1;

        for (var i = 0; i < values.Length; i++) {
            if (values[i])
                result = (Byte)(result | mask);
            mask = (Byte)(mask << 1);
        }

        var buffer = writer.GetSpan(1);
        buffer[0] = result;
        writer.Advance(1);
        return writer;
    }

    static public IBufferWriter<Byte> WriteBoolean(this IBufferWriter<Byte> writer, in Boolean value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        var buffer = writer.GetSpan(1);
        buffer[0] = value ? (Byte)1 : (Byte)0;
        writer.Advance(1);
        return writer;
    }

    static public IBufferWriter<Byte> WriteBytes(this IBufferWriter<Byte> writer, in ReadOnlySpan<Byte> value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        var buffer = writer.GetSpan(value.Length);
        value.CopyTo(buffer);
        writer.Advance(value.Length);
        return writer;
    }

    static public IBufferWriter<Byte> WriteChar(this IBufferWriter<Byte> writer, in Char value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        var buffer = writer.GetSpan(1);
        buffer[0] = (Byte)value;
        writer.Advance(1);
        return writer;
    }

    static public IBufferWriter<Byte> WriteDouble(this IBufferWriter<Byte> writer, in Double value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        var buffer = writer.GetSpan(sizeof(Double));
        WriteDoubleBigEndian(buffer, value);
        writer.Advance(sizeof(Double));
        return writer;
    }

    static public IBufferWriter<Byte> WriteFieldArray(this IBufferWriter<Byte> writer, IList<Object> fieldArray) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        if (fieldArray is null)
            throw new ArgumentNullException(nameof(fieldArray));

        using var buffer = new MemoryBufferWriter<Byte>();
        var items = fieldArray.Aggregate(seed: buffer, (accumulator, value) =>
            (MemoryBufferWriter<Byte>)accumulator.WriteFieldValue(value)
        );

        return writer
            .WriteInt32BE(items.WrittenMemory.Length)
            .WriteBytes(items.WrittenMemory.Span);
    }

    static public IBufferWriter<Byte> WriteFieldTable(this IBufferWriter<Byte> writer, IEnumerable<KeyValuePair<String, Object>> fieldTable) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        if (fieldTable is null)
            throw new ArgumentNullException(nameof(fieldTable));

        using var buffer = new MemoryBufferWriter<Byte>();
        var rows = fieldTable.Aggregate(seed: buffer, (accumulator, field) =>
            (MemoryBufferWriter<Byte>)accumulator
                .WriteShortString(field.Key)
                .WriteFieldValue(field.Value)
        );

        return writer
            .WriteUInt32BE((UInt32)rows.WrittenMemory.Length)
            .WriteBytes(rows.WrittenMemory.Span);
    }

    static public IBufferWriter<Byte> WriteFieldValue(this IBufferWriter<Byte> writer, in Object field) =>
        field switch {
            Boolean        value => writer.WriteChar('t').WriteBoolean(value),
            SByte          value => writer.WriteChar('b').WriteInt8(value),
            Byte           value => writer.WriteChar('B').WriteUInt8(value),
            Int16          value => writer.WriteChar('s').WriteInt16BE(value),
            UInt16         value => writer.WriteChar('u').WriteUInt16BE(value),
            Int32          value => writer.WriteChar('I').WriteInt32BE(value),
            UInt32         value => writer.WriteChar('i').WriteUInt32BE(value),
            Int64          value => writer.WriteChar('L').WriteInt64BE(value),
            UInt64         value => writer.WriteChar('l').WriteUInt64BE(value),
            Single         value => writer.WriteChar('f').WriteSingle(value),
            Double         value => writer.WriteChar('d').WriteDouble(value),
            //Decimal        value => writer.WriteChar('D').WriteDecimal(value), // TODO: encode and write decimal-value
            String         value => writer.WriteChar('S').WriteLongString(value),
            Byte[]         value => writer.WriteChar('x').WriteUInt32BE((UInt32)value.Length).WriteBytes(value),
            Object[]       value => writer.WriteChar('A').WriteFieldArray(value),
            DateTimeOffset value => writer.WriteChar('T').WriteUInt64BE((UInt64)value.ToUnixTimeSeconds()),
            DateTime       value => writer.WriteChar('T').WriteUInt64BE((UInt64)new DateTimeOffset(value).ToUnixTimeSeconds()),
            IEnumerable<KeyValuePair<String, Object>> value => writer.WriteChar('F').WriteFieldTable(value),
            null                 => writer.WriteChar('V'),
            _ => writer // TODO: unsupported data type present in table...
    };

    static public IBufferWriter<Byte> WriteInt8(this IBufferWriter<Byte> writer, in SByte value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        var buffer = writer.GetSpan(1);
        buffer[0] = (Byte)value;
        writer.Advance(1);
        return writer;
    }

    static public IBufferWriter<Byte> WriteInt16BE(this IBufferWriter<Byte> writer, in Int16 value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        var buffer = writer.GetSpan(sizeof(Int16));
        WriteInt16BigEndian(buffer, value);
        writer.Advance(sizeof(Int16));
        return writer;
    }

    static public IBufferWriter<Byte> WriteInt32BE(this IBufferWriter<Byte> writer, in Int32 value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        var buffer = writer.GetSpan(sizeof(Int32));
        WriteInt32BigEndian(buffer, value);
        writer.Advance(sizeof(Int32));
        return writer;
    }

    static public IBufferWriter<Byte> WriteInt64BE(this IBufferWriter<Byte> writer, in Int64 value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        var buffer = writer.GetSpan(sizeof(Int64));
        WriteInt64BigEndian(buffer, value);
        writer.Advance(sizeof(Int64));
        return writer;
    }

    static public IBufferWriter<Byte> WriteLongString(this IBufferWriter<Byte> writer, String value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        return writer.WriteUInt32BE((UInt32)UTF8.GetByteCount(value))
            .WriteBytes(UTF8.GetBytes(value));
    }

    static public IBufferWriter<Byte> WriteSerializable(this IBufferWriter<Byte> writer, ISerializable value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        if (value is null)
            throw new ArgumentNullException(nameof(value));

        return value.Serialize(writer);
    }

    static public IBufferWriter<Byte> WriteShortString(this IBufferWriter<Byte> writer, String value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        if (value.Length > Byte.MaxValue)
            throw new ArgumentException("Value is too long to be encoded as a short string", nameof(value));

        return writer.WriteUInt8((Byte)UTF8.GetByteCount(value))
            .WriteBytes(UTF8.GetBytes(value));
    }

    static public IBufferWriter<Byte> WriteSingle(this IBufferWriter<Byte> writer, in Single value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        var buffer = writer.GetSpan(sizeof(Single));
        WriteSingleBigEndian(buffer, value);
        writer.Advance(sizeof(Single));
        return writer;
    }

    static public IBufferWriter<Byte> WriteUInt8(this IBufferWriter<Byte> writer, in Byte value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        var buffer = writer.GetSpan(sizeof(Byte));
        buffer[0]  = value;
        writer.Advance(sizeof(Byte));
        return writer;
    }

    static public IBufferWriter<Byte> WriteUInt16BE(this IBufferWriter<Byte> writer, in UInt16 value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        var buffer = writer.GetSpan(sizeof(UInt16));
        WriteUInt16BigEndian(buffer, value);
        writer.Advance(sizeof(UInt16));
        return writer;
    }

    static public IBufferWriter<Byte> WriteUInt32BE(this IBufferWriter<Byte> writer, in UInt32 value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        var buffer = writer.GetSpan(sizeof(UInt32));
        WriteUInt32BigEndian(buffer, value);
        writer.Advance(sizeof(UInt32));
        return writer;
    }

    static public IBufferWriter<Byte> WriteUInt32LE(this IBufferWriter<Byte> writer, in UInt32 value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        var buffer = writer.GetSpan(sizeof(UInt32));
        WriteUInt32LittleEndian(buffer, value);
        writer.Advance(sizeof(UInt32));
        return writer;
    }

    static public IBufferWriter<Byte> WriteUInt64BE(this IBufferWriter<Byte> writer, in UInt64 value) {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        var buffer = writer.GetSpan(sizeof(UInt64));
        WriteUInt64BigEndian(buffer, value);
        writer.Advance(sizeof(UInt64));
        return writer;
    }
}
