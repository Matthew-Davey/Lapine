module Buffer

open System
open System.Buffers
open System.Buffers.Binary
open System.Text

type ReadOnlyMemory<'a> with
    member this.GetSlice(startIdx, endIdx) =
        let s = defaultArg startIdx 0
        let e = defaultArg endIdx this.Length
        this.Slice(s, e - s)

type DeserializerBuilder() =
    let run state (f: (ReadOnlyMemory<uint8> -> ReadOnlyMemory<uint8> * 'T)) =
        f state

    let bind binder f =
        fun state ->
            let state', result = f |> run state
            binder result |> run state'

    let (>>=) f binder = bind binder f
    member _.Return(result) = fun (state: ReadOnlyMemory<uint8>) -> (state, result)
    member _.ReturnFrom(f) = f
    member _.Bind(f, binder) = f >>= binder
    member this.Zero() = this.Return ()
    member _.Combine(a, b) =
        a >>= (fun _ -> b)
    member _.Delay(f) = f ()

let deserialize = DeserializerBuilder()

let readBits (buffer: ReadOnlyMemory<uint8>) =
    let bits = buffer.Span[0]
    (buffer.[1..], (bits &&& 0b00000001uy > 0uy,
                    bits &&& 0b00000010uy > 0uy,
                    bits &&& 0b00000100uy > 0uy,
                    bits &&& 0b00001000uy > 0uy,
                    bits &&& 0b00010000uy > 0uy,
                    bits &&& 0b00100000uy > 0uy,
                    bits &&& 0b01000000uy > 0uy,
                    bits &&& 0b10000000uy > 0uy))

let readBoolean (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[1..], buffer.Span[0] > 0uy)

let readChar (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[1..], char(buffer.Span[0]))

let readBytes (count: uint16) (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[int count..], buffer.[..int count])

let readUInt8 (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[1..], buffer.Span[0])

let readInt8 (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[1..], int8 buffer.Span[0])

let readUInt16BE (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[sizeof<uint16>..], BinaryPrimitives.ReadUInt16BigEndian(buffer.Span))

let readInt16BE (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[sizeof<int16>..], BinaryPrimitives.ReadInt16BigEndian(buffer.Span))

let readUInt32LE (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[sizeof<uint32>..], BinaryPrimitives.ReadUInt32LittleEndian(buffer.Span))

let readInt32LE (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[sizeof<int32>..], BinaryPrimitives.ReadInt32LittleEndian(buffer.Span))

let readUInt32BE (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[sizeof<uint32>..], BinaryPrimitives.ReadUInt32BigEndian(buffer.Span))

let readInt32BE (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[sizeof<int32>..], BinaryPrimitives.ReadInt32BigEndian(buffer.Span))

let readUInt64BE (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[sizeof<uint64>..], BinaryPrimitives.ReadUInt64BigEndian(buffer.Span))

let readInt64BE (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[sizeof<int64>..], BinaryPrimitives.ReadUInt64BigEndian(buffer.Span))

let readShortString (buffer: ReadOnlyMemory<uint8>) =
    let buffer, length = (readUInt8 buffer)
    (buffer.[int length..], Encoding.UTF8.GetString(buffer.[..int length].Span))

let readSingle (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[sizeof<single>..], BinaryPrimitives.ReadSingleBigEndian(buffer.Span))

let readDouble (buffer: ReadOnlyMemory<uint8>) =
    (buffer.[sizeof<double>..], BinaryPrimitives.ReadDoubleBigEndian(buffer.Span))

let readLongString (buffer: ReadOnlyMemory<uint8>) =
    let buffer, length = (readUInt32BE buffer)
    (buffer.[int length..], Encoding.UTF8.GetString(buffer.[..int length].Span))

let readDecimal (buffer: ReadOnlyMemory<uint8>) =
    // TODO: implementation
    (buffer.[5..], Decimal())

let readByteArray = deserialize {
    let! length = readUInt32BE
    return! readBytes (uint16 length)
}

let readTimestamp = deserialize {
    let! timestamp = readUInt16BE
    return DateTimeOffset.FromUnixTimeSeconds(int64 timestamp)
}

let rec readFieldValue = deserialize {
    let asObj (buffer, result) = buffer, result :> obj

    match! readChar with
    | 't' -> return! readBoolean >> asObj
    | 'b' -> return! readInt8 >> asObj
    | 'B' -> return! readUInt8 >> asObj
    | 's' -> return! readUInt16BE >> asObj
    | 'u' -> return! readUInt16BE >> asObj
    | 'I' -> return! readInt32BE >> asObj
    | 'i' -> return! readUInt32BE >> asObj
    | 'L' -> return! readInt64BE >> asObj
    | 'l' -> return! readUInt64BE >> asObj
    | 'f' -> return! readSingle >> asObj
    | 'd' -> return! readDouble >> asObj
    | 'D' -> return! readDecimal >> asObj
    | 'S' -> return! readLongString >> asObj
    | 'x' -> return! readByteArray >> asObj
    | 'A' -> return! readFieldArray >> asObj
    | 'T' -> return! readTimestamp >> asObj
    | 'V' -> return DBNull.Value
    | 'F' -> return! readFieldTable >> asObj
    | _ -> return failwith "Unknown field discriminator"
}
and readFieldArray = deserialize {
    let rec readFieldArray' (fields: obj list) (buffer: ReadOnlyMemory<uint8>) =
        match buffer.Length with
        | 0 -> fields
        | _ ->
            let buffer, fieldValue = readFieldValue buffer
            readFieldArray' (fields::[fieldValue]) buffer

    let! arrayLength = readUInt32BE
    let! arrayBytes = readBytes (uint16 arrayLength)

    return readFieldArray' List.empty arrayBytes
}
and readFieldTable = deserialize {
    let rec readFieldTable' (fields: Map<string, obj>) (buffer: ReadOnlyMemory<uint8>) =
        match buffer.Length with
        | 0 -> fields
        | _ ->
            let buffer, fieldName = readShortString buffer
            let buffer, fieldValue = readFieldValue buffer
            readFieldTable' (fields |> Map.add fieldName fieldValue) buffer

    let! tableLength = readUInt32BE
    let! tableBytes = readBytes (uint16 tableLength)

    return readFieldTable' Map.empty tableBytes
}

let writeBoolean (value: bool) (writer: IBufferWriter<uint8>) =
    let buffer = writer.GetSpan 1
    buffer.[0] <- match value with | true -> 1uy | false -> 0uy
    writer.Advance 1
    writer

let writeBytes (value: ReadOnlyMemory<uint8>) (writer: IBufferWriter<uint8>) =
    let buffer = writer.GetSpan(value.Length)
    writer.Write(value.Span)
    writer

let writeUInt8 (value: uint8) (writer: IBufferWriter<uint8>) =
    let buffer = writer.GetSpan(sizeof<uint8>)
    buffer[0] <- value
    writer.Advance(sizeof<uint8>)
    writer

let writeUInt16BE (value: uint16) (writer: IBufferWriter<uint8>) =
    let buffer = writer.GetSpan(sizeof<uint16>)
    BinaryPrimitives.WriteUInt16BigEndian(buffer, value)
    writer.Advance(sizeof<uint16>)
    writer

let writeUInt32LE (value: uint32) (writer: IBufferWriter<uint8>) =
    let buffer = writer.GetSpan(sizeof<uint32>)
    BinaryPrimitives.WriteUInt32LittleEndian(buffer, value)
    writer.Advance(sizeof<uint32>)
    writer

let writeUInt32BE (value: uint32) (writer : IBufferWriter<uint8>) =
    let buffer = writer.GetSpan(sizeof<uint32>)
    BinaryPrimitives.WriteUInt32BigEndian(buffer, value)
    writer.Advance(sizeof<uint32>)
    writer

let writeUInt64BE (value: uint64) (writer: IBufferWriter<uint8>) =
    let buffer = writer.GetSpan(sizeof<uint64>)
    BinaryPrimitives.WriteUInt64BigEndian(buffer, value)
    writer.Advance(sizeof<uint64>)
    writer

let writeShortString (value: string) =
    writeUInt8 (uint8 (Encoding.UTF8.GetByteCount(value)))
    >> writeBytes(ReadOnlyMemory<uint8>.op_Implicit (Encoding.UTF8.GetBytes(value)))

let writeLongString (value: string) =
    writeUInt32BE (uint32 (Encoding.UTF8.GetByteCount(value)))
    >> writeBytes(ReadOnlyMemory<uint8>.op_Implicit (Encoding.UTF8.GetBytes(value)))

// TODO: implementation
let writeFieldTable (value: Map<string, obj>) =
    writeUInt32BE 0u
