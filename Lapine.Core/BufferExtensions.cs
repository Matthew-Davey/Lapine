namespace Lapine
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;

    using static System.Buffers.Binary.BinaryPrimitives;
    using static System.Text.Encoding;

    public static class BufferExtensions {
        static public Boolean ReadBits(in this ReadOnlySpan<Byte> buffer, out Boolean[] result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.Length < 1) {
                result = default;
                surplus = default;
                return false;
            }

            result = new Boolean[8];

            var bits = buffer[0];
            var mask = 0x01;

            for (var i = 0; i < 8; i++) {
                result[i] = (bits & mask) != 0;
                mask = mask << 1;
            }

            surplus = buffer.Slice(1);
            return true;
        }

        static public Boolean ReadBoolean(in this ReadOnlySpan<Byte> buffer, out Boolean result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.Length < 1) {
                result  = default;
                surplus = default;
                return false;
            }

            result  = buffer[0] > 0;
            surplus = buffer.Slice(1);
            return true;
        }

        static public Boolean ReadBytes(in this ReadOnlySpan<Byte> buffer, in UInt32 number, out ReadOnlySpan<Byte> result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.Length < number) {
                result  = default;
                surplus = default;
                return false;
            }

            result  = buffer.Slice(0, (Int32)number);
            surplus = buffer.Slice((Int32)number);
            return true;
        }

        static public Boolean ReadChar(in this ReadOnlySpan<Byte> buffer, out Char result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.Length < 1) {
                result  = default;
                surplus = default;
                return false;
            }

            result  = (Char)buffer[0];
            surplus = buffer.Slice(1);
            return true;
        }

        static public Boolean ReadChars(in this ReadOnlySpan<Byte> buffer, in UInt16 number, out ReadOnlySpan<Char> result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.Length < number) {
                result  = default;
                surplus = default;
                return false;
            }

            result  = ASCII.GetString(buffer.Slice(0, number)).AsSpan();
            surplus = buffer.Slice(number);
            return true;
        }

        static public Boolean ReadDouble(in this ReadOnlySpan<Byte> buffer, out Double result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadBytes(sizeof(Double), out var rawBytes, out surplus)) {
                result = BitConverter.ToDouble(rawBytes);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }

        static public Boolean ReadFieldArray(in this ReadOnlySpan<Byte> buffer, out IList<Object> result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadInt32BE(out var arrayLength, out surplus) &&
                surplus.ReadBytes((UInt32)arrayLength, out var arrayBytes, out surplus))
            {
                var fieldArray = new List<Object>();

                while (arrayBytes.Length > 0) {
                    if (arrayBytes.ReadFieldValue(out var fieldValue, out arrayBytes)) {
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

        static public Boolean ReadFieldTable(in this ReadOnlySpan<Byte> buffer, out IReadOnlyDictionary<String, Object> result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt32BE(out var tableLength, out surplus) &&
                surplus.ReadBytes(tableLength, out var tableBytes, out surplus))
            {
                var fields = new Dictionary<String, Object>();

                while (tableBytes.Length > 0) {
                    if (tableBytes.ReadShortString(out var fieldName, out tableBytes) &&
                        tableBytes.ReadFieldValue(out var fieldValue, out tableBytes))
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

        static public Boolean ReadFieldValue(in this ReadOnlySpan<Byte> buffer, out Object result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadChar(out var fieldType, out surplus) == false) {
                result = default;
                return false;
            }

            switch (fieldType) {
                case 't': { // boolean
                    if (surplus.ReadBoolean(out var value, out surplus)) {
                        result = value;
                        return true;
                    }
                    break;
                }
                case 'b': { // short-short-int
                    if (surplus.ReadInt8(out var value, out surplus)) {
                        result = value;
                        return true;
                    }
                    break;
                }
                case 'B': { // short-short-uint
                    if (surplus.ReadUInt8(out var value, out surplus)) {
                        result = value;
                        return true;
                    }
                    break;
                }
                case 'U': { // short-int
                    if (surplus.ReadInt16BE(out var value, out surplus)) {
                        result = value;
                        return true;
                    }
                    break;
                }
                case 'u': { // short-uint
                    if (surplus.ReadUInt16BE(out var value, out surplus)) {
                        result = value;
                        return true;
                    }
                    break;
                }
                case 'I': { // long-int
                    if (surplus.ReadInt32BE(out var value, out surplus)) {
                        result = value;
                        return true;
                    }
                    break;
                }
                case 'i': { // long-uint
                    if (surplus.ReadUInt32BE(out var value, out surplus)) {
                        result = value;
                        return true;
                    }
                    break;
                }
                case 'L': { // long-long-int
                    if (surplus.ReadInt64BE(out var value, out surplus)) {
                        result = value;
                        return true;
                    }
                    break;
                }
                case 'l': { // long-long-uint
                    if (surplus.ReadUInt32BE(out var value, out surplus)) {
                        result = value;
                        return true;
                    }
                    break;
                }
                case 'f': { // float
                    if (surplus.ReadSingle(out var value, out surplus)) {
                        result = value;
                        return true;
                    }
                    break;
                }
                case 'd': { // double
                    if (surplus.ReadDouble(out var value, out surplus)) {
                        result = value;
                        return true;
                    }
                    break;
                }
                case 'D': { // decimal-value
                    result = default(Decimal); // TODO: read and decode decimal-value
                    surplus = surplus.Slice(5);
                    return true;
                }
                case 's': { // short-string
                    if (surplus.ReadShortString(out var value, out surplus)) {
                        result = value;
                        return true;
                    }
                    break;
                }
                case 'S': { // long-string
                    if (surplus.ReadLongString(out var value, out surplus)) {
                        result = value;
                        return true;
                    }
                    break;
                }
                case 'A' : { // field-array
                    break; // TODO: read and decode field-array
                }
                case 'T': { // timestamp
                    if (surplus.ReadUInt64BE(out var value, out surplus)) {
                        result = DateTimeOffset.FromUnixTimeSeconds((Int64)value);
                        return true;
                    }
                    break;
                }
                case 'V': { // no-field
                    result = null;
                    return true;
                }
                case 'F': { // field-table
                    if (surplus.ReadFieldTable(out var value, out surplus)) {
                        result = value;
                        return true;
                    }
                    break;
                }
            }

            result = default;
            return false;
        }

        static public Boolean ReadInt8(in this ReadOnlySpan<Byte> buffer, out SByte result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.Length < 1) {
                result  = default;
                surplus = default;
                return false;
            }

            result = (SByte)buffer[0];
            surplus = buffer.Slice(1);
            return true;
        }

        static public Boolean ReadInt16BE(in this ReadOnlySpan<Byte> buffer, out Int16 result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.Length < sizeof(Int16)) {
                result  = default;
                surplus = default;
                return false;
            }

            result  = ReadInt16BigEndian(buffer);
            surplus = buffer.Slice(sizeof(Int16));
            return true;
        }

        static public Boolean ReadInt32BE(in this ReadOnlySpan<Byte> buffer, out Int32 result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.Length < sizeof(Int32)) {
                result  = default;
                surplus = default;
                return false;
            }

            result  = ReadInt32BigEndian(buffer);
            surplus = buffer.Slice(sizeof(Int32));
            return true;
        }

        static public Boolean ReadInt64BE(in this ReadOnlySpan<Byte> buffer, out Int64 result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.Length < sizeof(Int64)) {
                result  = default;
                surplus = default;
                return false;
            }

            result  = ReadInt64BigEndian(buffer);
            surplus = buffer.Slice(sizeof(Int64));
            return true;
        }

        static public Boolean ReadLongString(in this ReadOnlySpan<Byte> buffer, out String result, out ReadOnlySpan<Byte> surplus) {
            if (ReadUInt32BE(in buffer, out var length, out surplus) &&
                ReadBytes(in surplus, length, out var bytes, out surplus))
            {
                result = UTF8.GetString(bytes);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }

        static public Boolean ReadMethodHeader(in this ReadOnlySpan<Byte> buffer, out (UInt16 ClassId, UInt16 MethodId) result, out ReadOnlySpan<Byte> surplus) {
            if (ReadUInt16BE(in buffer, out var classId, out surplus) &&
                ReadUInt16BE(in surplus, out var methodId, out surplus))
            {
                result = (classId, methodId);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }

        static public Boolean ReadShortString(in this ReadOnlySpan<Byte> buffer, out String result, out ReadOnlySpan<Byte> surplus) {
            if (ReadUInt8(in buffer, out var length, out surplus) &&
                ReadBytes(in surplus, length, out var bytes, out surplus))
            {
                result = UTF8.GetString(bytes);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }

        static public Boolean ReadSingle(in this ReadOnlySpan<Byte> buffer, out Single result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadBytes(sizeof(Single), out var rawBytes, out surplus)) {
                result = BitConverter.ToSingle(rawBytes);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }

        static public Boolean ReadUInt8(in this ReadOnlySpan<Byte> buffer, out Byte result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.Length < sizeof(Byte)) {
                result  = default;
                surplus = default;
                return false;
            }

            result  = buffer[0];
            surplus = buffer.Slice(sizeof(Byte));
            return true;
        }

        static public Boolean ReadUInt16BE(in this ReadOnlySpan<Byte> buffer, out UInt16 result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.Length < sizeof(UInt16)) {
                result  = default;
                surplus = default;
                return false;
            }

            result  = ReadUInt16BigEndian(buffer);
            surplus = buffer.Slice(sizeof(UInt16));
            return true;
        }

        static public Boolean ReadUInt32BE(in this ReadOnlySpan<Byte> buffer, out UInt32 result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.Length < sizeof(UInt32)) {
                result  = default;
                surplus = default;
                return false;
            }

            result  = ReadUInt32BigEndian(buffer);
            surplus = buffer.Slice(sizeof(UInt32));
            return true;
        }

        static public Boolean ReadUInt64BE(in this ReadOnlySpan<Byte> buffer, out UInt64 result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.Length < sizeof(UInt64)) {
                result  = default;
                surplus = default;
                return false;
            }

            result  = ReadUInt64BigEndian(buffer);
            surplus = buffer.Slice(sizeof(UInt64));
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

        static public IBufferWriter<Byte> WriteDouble(this IBufferWriter<Byte> writer, in Double value) =>
            writer.WriteBytes(BitConverter.GetBytes(value));

        static public IBufferWriter<Byte> WriteFieldArray(this IBufferWriter<Byte> writer, IList<Object> fieldArray) {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));

            if (fieldArray is null)
                throw new ArgumentNullException(nameof(fieldArray));

            var arrayBuffer = new ArrayBufferWriter<Byte>();

            fieldArray.Aggregate(seed: arrayBuffer as IBufferWriter<Byte>, (arrayBuffer, value) =>
                arrayBuffer.WriteFieldValue(value)
            );

            return writer.WriteInt32BE((Int32)arrayBuffer.WrittenMemory.Length)
                .WriteBytes(arrayBuffer.WrittenMemory.Span);
        }

        static public IBufferWriter<Byte> WriteFieldTable(this IBufferWriter<Byte> writer, IReadOnlyDictionary<String, Object> fieldTable) {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));

            if (fieldTable is null)
                throw new ArgumentNullException(nameof(fieldTable));

            var tablebuffer = new ArrayBufferWriter<Byte>();

            fieldTable.Aggregate(seed: tablebuffer as IBufferWriter<Byte>, (tablebuffer, field) =>
                tablebuffer.WriteShortString(field.Key)
                    .WriteFieldValue(field.Value)
            );

            return writer.WriteUInt32BE((UInt32)tablebuffer.WrittenMemory.Length)
                .WriteBytes(tablebuffer.WrittenMemory.Span);
        }

        static public IBufferWriter<Byte> WriteFieldValue(this IBufferWriter<Byte> writer, in Object field) =>
            field switch {
                Boolean        value => writer.WriteChar('t').WriteBoolean(value),
                SByte          value => writer.WriteChar('b').WriteInt8(value),
                Byte           value => writer.WriteChar('B').WriteUInt8(value),
                Int16          value => writer.WriteChar('U').WriteInt16BE(value),
                UInt16         value => writer.WriteChar('u').WriteUInt16BE(value),
                Int32          value => writer.WriteChar('I').WriteInt32BE(value),
                UInt32         value => writer.WriteChar('i').WriteUInt32BE(value),
                Int64          value => writer.WriteChar('L').WriteInt64BE(value),
                UInt64         value => writer.WriteChar('l').WriteUInt64BE(value),
                Single         value => writer.WriteChar('f').WriteSingle(value),
                Double         value => writer.WriteChar('d').WriteDouble(value),
                //Decimal        value => writer.WriteChar('D').WriteDecimal(value), // TODO: encode and write decimal-value
                String         value when value.Length < 256 => writer.WriteChar('s').WriteShortString(value),
                String         value when value.Length > 255 => writer.WriteChar('S').WriteLongString(value),
                //Object[]       value => writer.WriteChar('A').WriteFieldArray(value), // TODO: encode and write field-array
                DateTimeOffset value => writer.WriteChar('T').WriteUInt64BE((UInt64)value.ToUnixTimeSeconds()),
                DateTime       value => writer.WriteChar('T').WriteUInt64BE((UInt64)new DateTimeOffset(value).ToUnixTimeSeconds()),
                IReadOnlyDictionary<String, Object> value => writer.WriteChar('F').WriteFieldTable(value),
                //null                 => writer.WriteChar('V'), // TODO: what does 'V' mean?
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

            return writer.WriteUInt32BE((UInt32)value.Length)
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

            return writer.WriteUInt8((Byte)value.Length)
                .WriteBytes(UTF8.GetBytes(value));
        }

        static public IBufferWriter<Byte> WriteSingle(this IBufferWriter<Byte> writer, in Single value) =>
            writer.WriteBytes(BitConverter.GetBytes(value));

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
}
