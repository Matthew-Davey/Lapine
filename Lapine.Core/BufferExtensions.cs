namespace Lapine
{
    using System;
    using System.Buffers;

    using static System.Buffers.Binary.BinaryPrimitives;
    using static System.Text.Encoding;

    public static class BufferExtensions {
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

        static public Boolean ReadBytes(in this ReadOnlySpan<Byte> buffer, in UInt32 number, out ReadOnlySpan<Byte> result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.Length < number) {
                result  = default;
                surplus = default;
                return false;
            }

            result    = buffer.Slice(0, (Int32)number);
            surplus = buffer.Slice((Int32)number);
            return true;
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

        static public IBufferWriter<Byte> WriteSerializable(this IBufferWriter<Byte> writer, ISerializable value) {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));

            if (value is null)
                throw new ArgumentNullException(nameof(value));

            return value.Serialize(writer);
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
}
