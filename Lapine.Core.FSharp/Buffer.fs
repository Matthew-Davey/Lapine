module Buffer

open System
open System.Buffers

open type System.Buffers.Binary.BinaryPrimitives
open type System.Text.Encoding

type float64 = Double

type ReadOnlyMemory<'a> with
    member this.GetSlice (startIdx, endIdx) =
        let s = defaultArg startIdx 0
        let e = defaultArg endIdx this.Length
        this.Slice (s, e - s)

type DeserializerBuilder() =
    let run state (f: ReadOnlyMemory<uint8> -> ReadOnlyMemory<uint8> * 'T) =
        f state

    let bind binder f =
        fun state ->
            let state', result = f |> run state
            binder result |> run state'

    let (>>=) f binder = bind binder f
    member _.Return result = fun (state: ReadOnlyMemory<uint8>) -> (state, result)
    member _.ReturnFrom f = f
    member _.Bind (f, binder) = f >>= binder
    member this.Zero () = this.Return ()
    member _.Combine (a, b) =
        a >>= (fun _ -> b)
    member _.Delay f = f ()

let deserialize = DeserializerBuilder ()

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
    (buffer[1..], buffer.Span[0] > 0uy)

let readChar (buffer: ReadOnlyMemory<uint8>) =
    (buffer[1..], char (buffer.Span[0]))

let readBytes (count: uint16) (buffer: ReadOnlyMemory<uint8>) =
    (buffer[int count..], buffer[..int count])

let readUInt8 (buffer: ReadOnlyMemory<uint8>) =
    (buffer[1..], buffer.Span[0])

let readInt8 (buffer: ReadOnlyMemory<uint8>) =
    (buffer[1..], int8 buffer.Span[0])

let readUInt16BE (buffer: ReadOnlyMemory<uint8>) =
    (buffer[sizeof<uint16>..], ReadUInt16BigEndian buffer.Span)

let readInt16BE (buffer: ReadOnlyMemory<uint8>) =
    (buffer[sizeof<int16>..], ReadInt16BigEndian buffer.Span)

let readUInt32LE (buffer: ReadOnlyMemory<uint8>) =
    (buffer[sizeof<uint32>..], ReadUInt32LittleEndian buffer.Span)

let readInt32LE (buffer: ReadOnlyMemory<uint8>) =
    (buffer[sizeof<int32>..], ReadInt32LittleEndian buffer.Span)

let readUInt32BE (buffer: ReadOnlyMemory<uint8>) =
    (buffer[sizeof<uint32>..], ReadUInt32BigEndian buffer.Span)

let readInt32BE (buffer: ReadOnlyMemory<uint8>) =
    (buffer[sizeof<int32>..], ReadInt32BigEndian buffer.Span)

let readUInt64BE (buffer: ReadOnlyMemory<uint8>) =
    (buffer[sizeof<uint64>..], ReadUInt64BigEndian buffer.Span)

let readInt64BE (buffer: ReadOnlyMemory<uint8>) =
    (buffer[sizeof<int64>..], ReadUInt64BigEndian buffer.Span)

let readShortString (buffer: ReadOnlyMemory<uint8>) =
    let buffer, length = readUInt8 buffer
    (buffer[int length..], UTF8.GetString buffer[..int length].Span)

let readSingle (buffer: ReadOnlyMemory<uint8>) =
    (buffer[sizeof<single>..], ReadSingleBigEndian buffer.Span)

let readDouble (buffer: ReadOnlyMemory<uint8>) =
    (buffer[sizeof<double>..], ReadDoubleBigEndian buffer.Span)

let readLongString (buffer: ReadOnlyMemory<uint8>) =
    let buffer, length = readUInt32BE buffer
    (buffer[int length..], UTF8.GetString buffer[..int length].Span)

let readDecimal (buffer: ReadOnlyMemory<uint8>) =
    // TODO: implementation
    (buffer[5..], Decimal())

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
    buffer[0] <- match value with | true -> 1uy | false -> 0uy
    writer.Advance 1
    writer
    
let writeBytes (value: ReadOnlyMemory<uint8>) (writer: IBufferWriter<uint8>) =
    let buffer = writer.GetSpan value.Length
    value.Span.CopyTo buffer
    writer.Advance value.Length
    writer

let writeUInt8 (value: uint8) (writer: IBufferWriter<uint8>) =
    let buffer = writer.GetSpan sizeof<uint8>
    buffer[0] <- value
    writer.Advance sizeof<uint8>
    writer
    
let writeInt8 (value: int8) (writer: IBufferWriter<uint8>) =
    let buffer = writer.GetSpan sizeof<int8>
    buffer[0] <- uint8 value
    writer.Advance sizeof<int8>
    writer

let writeChar (value: char) (writer: IBufferWriter<uint8>) =
    let buffer = writer.GetSpan 1
    buffer[0] <- uint8 value
    writer.Advance 1
    writer

let writeUInt16BE (value: uint16) (writer: IBufferWriter<uint8>) =
    let buffer = writer.GetSpan sizeof<uint16>
    WriteUInt16BigEndian (buffer, value)
    writer.Advance sizeof<uint16>
    writer
    
let writeInt16BE (value: int16) (writer: IBufferWriter<uint8>) =
    let buffer = writer.GetSpan sizeof<int16>
    WriteInt16BigEndian (buffer, value)
    writer.Advance sizeof<int16>
    writer

let writeUInt32LE (value: uint32) (writer: IBufferWriter<uint8>) =
    let buffer = writer.GetSpan sizeof<uint32>
    WriteUInt32LittleEndian (buffer, value)
    writer.Advance sizeof<uint32>
    writer

let writeUInt32BE (value: uint32) (writer : IBufferWriter<uint8>) =
    let buffer = writer.GetSpan sizeof<uint32>
    WriteUInt32BigEndian (buffer, value)
    writer.Advance sizeof<uint32>
    writer

let writeUInt64BE (value: uint64) (writer: IBufferWriter<uint8>) =
    let buffer = writer.GetSpan sizeof<uint64>
    WriteUInt64BigEndian (buffer, value)
    writer.Advance sizeof<uint64>
    writer

let writeShortString (value: string) =
    writeUInt8 (uint8 (UTF8.GetByteCount value))
    >> writeBytes (ReadOnlyMemory<uint8>.op_Implicit (UTF8.GetBytes value))

let writeLongString (value: string) =
    writeUInt32BE (uint32 (UTF8.GetByteCount value))
    >> writeBytes (ReadOnlyMemory<uint8>.op_Implicit (UTF8.GetBytes value))

let rec writeFieldValue (value: obj) =
    match value with
    | :? bool as value -> writeChar 't' >> writeBoolean value
    | :? int8 as value -> writeChar 'b' >> writeInt8 value
    | :? uint8 as value -> writeChar 'B' >> writeUInt8 value
    | :? int16 as value -> writeChar 's' >> writeInt16BE value
    | :? uint16 as value -> writeChar 'u' >> writeUInt16BE value
    //| :? int32 as value -> writeChar 'I' >> writeInt32BE value
    | :? uint32 as value -> writeChar 'i' >> writeUInt32BE value
    //| :? int64 as value -> writeChar 'L' >> writeInt64BE value
    | :? uint64 as value -> writeChar 'l' >> writeUInt64BE value
    //| :? float32 as value -> writeChar 'f' >> writeFloat32 value
    //| :? float64 as value -> writeChar 'd' >> writeFloat64 value
    //| :? decimal as value -> writeChar 'D' >> writeDecimal value
    | :? string as value -> writeChar 'S' >> writeLongString value
    | :? array<uint8> as value -> writeChar 'x' >> writeBytes value
    | :? seq<obj> as value -> writeChar 'A' >> writeFieldArray value
    | :? DateTimeOffset as value -> writeChar 'T' >> writeUInt64BE (uint64 (value.ToUnixTimeSeconds()))
    | :? Map<string, obj> as value -> writeChar 'F' >> writeFieldTable value
    | :? Map<string, bool> as value -> writeChar 'F' >> writeFieldTable (Map.map (fun _ item -> item :> obj) value)
    | null -> writeChar 'V'
    | _ -> failwith "Unsupported field value"
and writeFieldArray (value: obj seq) =
    writeUInt8 0uy
and writeFieldTable (value: Map<string, obj>) =
    let buffer = ArrayBufferWriter<uint8>()
    let rows = Map.fold (fun accumulator field value -> accumulator |> writeShortString field |> writeFieldValue value) buffer value
               :?> ArrayBufferWriter<uint8>
    writeUInt32BE (uint32 rows.WrittenMemory.Length)
    >> writeBytes rows.WrittenMemory
