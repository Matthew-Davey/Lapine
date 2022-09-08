module Amqp

open System
open System.Buffers
open System.Text

open Buffer

type ProtocolVersion = { Major: byte; Minor: byte; Revision: byte } with
    static member Default = { Major = 0uy; Minor = 9uy; Revision = 1uy }
    static member Deserialize = deserialize {
        let! major    = readUInt8
        let! minor    = readUInt8
        let! revision = readUInt8

        return { Major = major; Minor = minor; Revision = revision }
    }
    static member Serialize header =
        writeUInt8 header.Major
        >> writeUInt8 header.Minor
        >> writeUInt8 header.Revision
end

type ProtocolHeader = { Protocol: uint32; ProtocolId: byte; Version: ProtocolVersion } with
    static member Create protocol protocolId version =
        match String.length protocol with
        | 4 -> {
                   Protocol   = BitConverter.ToUInt32(Encoding.ASCII.GetBytes(protocol))
                   ProtocolId = protocolId
                   Version    = version
               }
        | _ -> failwith "value must be exactly four characters long"
    static member Default = ProtocolHeader.Create "AMQP" 0uy ProtocolVersion.Default
    static member Serialize header =
        writeUInt32LE header.Protocol
        >> writeUInt8 header.ProtocolId
        >> ProtocolVersion.Serialize header.Version
end

type MethodHeader = { ClassId: uint16; MethodId: uint16 } with
    static member Deserialize = deserialize {
        let! classId  = readUInt16BE
        let! methodId = readUInt16BE

        return { ClassId = classId; MethodId = methodId }
    }
    static member Serialize header =
        writeUInt16BE header.ClassId
        >> writeUInt16BE header.MethodId
end

[<Flags>]
type PropertyFlags =
    | None            = 0b0000000000000000us
    | ContentType     = 0b1000000000000000us
    | ContentEncoding = 0b0100000000000000us
    | Headers         = 0b0010000000000000us
    | DeliveryMode    = 0b0001000000000000us
    | Priority        = 0b0000100000000000us
    | CorrelationId   = 0b0000010000000000us
    | ReplyTo         = 0b0000001000000000us
    | Expiration      = 0b0000000100000000us
    | MessageId       = 0b0000000010000000us
    | Timestamp       = 0b0000000001000000us
    | Type            = 0b0000000000100000us
    | UserId          = 0b0000000000010000us
    | AppId           = 0b0000000000001000us
    | ClusterId       = 0b0000000000000100us

type BasicProperties = {
    ContentType: string option
    ContentEncoding: string option
    Headers: Map<string, Object> option
    DeliveryMode: byte option
    Priority: byte option
    CorrelationId: string option
    ReplyTo: string option
    Expiration: string option
    MessageId: string option
    Timestamp: uint64 option
    Type: string option
    UserId: string option
    AppId: string option
    ClusterId: string option
} with
    static member None = {
        ContentType     = None
        ContentEncoding = None
        Headers         = None
        DeliveryMode    = None
        Priority        = None
        CorrelationId   = None
        ReplyTo         = None
        Expiration      = None
        MessageId       = None
        Timestamp       = None
        Type            = None
        UserId          = None
        AppId           = None
        ClusterId       = None
    }
    static member Flags properties =
        PropertyFlags.None
        ||| (match properties.ContentType     with | Some _ -> PropertyFlags.ContentType     | None -> PropertyFlags.None)
        ||| (match properties.ContentEncoding with | Some _ -> PropertyFlags.ContentEncoding | None -> PropertyFlags.None)
        ||| (match properties.Headers         with | Some _ -> PropertyFlags.Headers         | None -> PropertyFlags.None)
        ||| (match properties.DeliveryMode    with | Some _ -> PropertyFlags.DeliveryMode    | None -> PropertyFlags.None)
        ||| (match properties.Priority        with | Some _ -> PropertyFlags.Priority        | None -> PropertyFlags.None)
        ||| (match properties.CorrelationId   with | Some _ -> PropertyFlags.CorrelationId   | None -> PropertyFlags.None)
        ||| (match properties.ReplyTo         with | Some _ -> PropertyFlags.ReplyTo         | None -> PropertyFlags.None)
        ||| (match properties.Expiration      with | Some _ -> PropertyFlags.Expiration      | None -> PropertyFlags.None)
        ||| (match properties.MessageId       with | Some _ -> PropertyFlags.MessageId       | None -> PropertyFlags.None)
        ||| (match properties.Timestamp       with | Some _ -> PropertyFlags.Timestamp       | None -> PropertyFlags.None)
        ||| (match properties.Type            with | Some _ -> PropertyFlags.Type            | None -> PropertyFlags.None)
        ||| (match properties.UserId          with | Some _ -> PropertyFlags.UserId          | None -> PropertyFlags.None)
        ||| (match properties.AppId           with | Some _ -> PropertyFlags.AppId           | None -> PropertyFlags.None)
        ||| (match properties.ClusterId       with | Some _ -> PropertyFlags.ClusterId       | None -> PropertyFlags.None)
    static member Serialize properties =
        writeUInt16BE (uint16 (BasicProperties.Flags properties))
        >> (match properties.ContentType with     | Some contentType   -> writeShortString contentType  | None -> id)
        >> (match properties.ContentEncoding with | Some encoding      -> writeShortString encoding     | None -> id)
        >> (match properties.Headers with         | Some headers       -> writeFieldTable headers       | None -> id)
        >> (match properties.DeliveryMode with    | Some mode          -> writeUInt8 mode               | None -> id)
        >> (match properties.Priority with        | Some priority      -> writeUInt8 priority           | None -> id)
        >> (match properties.CorrelationId with   | Some correlationId -> writeLongString correlationId | None -> id)
        >> (match properties.ReplyTo with         | Some replyTo       -> writeLongString replyTo       | None -> id)
        >> (match properties.Expiration with      | Some expiration    -> writeLongString expiration    | None -> id)
        >> (match properties.MessageId with       | Some messageId     -> writeLongString messageId     | None -> id)
        >> (match properties.Timestamp with       | Some timestamp     -> writeUInt64BE timestamp       | None -> id)
        >> (match properties.Type with            | Some type'         -> writeLongString type'         | None -> id)
        >> (match properties.UserId with          | Some userId        -> writeLongString userId        | None -> id)
        >> (match properties.AppId with           | Some appId         -> writeLongString appId         | None -> id)
        >> (match properties.ClusterId with       | Some clusterId     -> writeLongString clusterId     | None -> id)
    static member Deserialize = deserialize {
        let! flags' = readUInt16BE
        let flags: PropertyFlags = LanguagePrimitives.EnumOfValue flags'

        let mutable properties = BasicProperties.None

        if flags.HasFlag PropertyFlags.ContentType then
            let! contentType = readShortString
            properties <- { properties with ContentType = Some contentType }

        if flags.HasFlag PropertyFlags.ContentEncoding then
            let! encoding = readShortString
            properties <- { properties with ContentEncoding = Some encoding }

        if flags.HasFlag PropertyFlags.Headers then
            let! headers = readFieldTable
            properties <- { properties with Headers = Some headers }

        if flags.HasFlag PropertyFlags.DeliveryMode then
            let! mode = readUInt8
            properties <- { properties with DeliveryMode = Some mode }

        if flags.HasFlag PropertyFlags.Priority then
            let! priority = readUInt8
            properties <- { properties with Priority = Some priority }

        if flags.HasFlag PropertyFlags.CorrelationId then
            let! correlationId = readLongString
            properties <- { properties with CorrelationId = Some correlationId }

        if flags.HasFlag PropertyFlags.ReplyTo then
            let! replyTo = readLongString
            properties <- { properties with ReplyTo = Some replyTo }

        if flags.HasFlag PropertyFlags.Expiration then
            let! expiration = readLongString
            properties <- { properties with Expiration = Some expiration }

        if flags.HasFlag PropertyFlags.MessageId then
            let! messageId = readLongString
            properties <- { properties with MessageId = Some messageId }

        if flags.HasFlag PropertyFlags.Timestamp then
            let! timestamp = readUInt64BE
            properties <- { properties with Timestamp = Some timestamp }

        if flags.HasFlag PropertyFlags.Type then
            let! type' = readLongString
            properties <- { properties with Type = Some type' }

        if flags.HasFlag PropertyFlags.UserId then
            let! userId = readLongString
            properties <- { properties with UserId = Some userId }

        if flags.HasFlag PropertyFlags.AppId then
            let! appId = readLongString
            properties <- { properties with AppId = Some appId }

        if flags.HasFlag PropertyFlags.ClusterId then
            let! clusterId = readLongString
            properties <- { properties with ClusterId = Some clusterId }

        return properties
    }
end

type ContentHeader = { ClassId: uint16; BodySize: uint64; Properties: BasicProperties } with
    static member Serialize header =
        writeUInt16BE header.ClassId
        >> writeUInt64BE header.BodySize
        >> BasicProperties.Serialize header.Properties
    static member Deserialize = deserialize {
        let! classId    = readUInt16BE
        let! bodySize   = readUInt64BE
        let! properties = BasicProperties.Deserialize

        return { ClassId = classId; BodySize = bodySize; Properties = properties }
    }
end

type Method =
    | ConnectionStart    of {| Version: {| Major: uint8; Minor: uint8  |}; ServerProperties: Map<string, obj>; Mechanisms: string list; Locales: string list |}
    | ConnectionStartOk  of {| PeerProperties: Map<string, obj>; Mechanism: string; Response: string; Locale: string |}
    | ConnectionSecure   of {| Challenge: string |}
    | ConnectionSecureOk of {| Response: string |}
    | ConnectionTune     of {| ChannelMax: uint16; FrameMax: uint32; Heartbeat: uint16 |}
    | ConnectionTuneOk   of {| ChannelMax: uint16; FrameMax: uint32; Heartbeat: uint16 |}
    | ConnectionOpen     of {| VirtualHost: string |}
    | ConnectionOpenOk with
    static member Deserialize = deserialize {
        match! MethodHeader.Deserialize with
        | { ClassId = 0x0Aus; MethodId = 0x0Aus } ->
            let! major            = readUInt8
            let! minor            = readUInt8
            let! serverProperties = readFieldTable
            let! mechanisms       = readLongString
            let! locales          = readLongString

            return ConnectionStart {|
                Version          = {| Major = major; Minor = minor |}
                ServerProperties = serverProperties
                Mechanisms       = mechanisms.Split(" ") |> List.ofArray
                Locales          = locales.Split(" ") |> List.ofArray
            |}
        | { ClassId = 0x0Aus; MethodId = 0x014us } ->
            let! challenge = readLongString

            return ConnectionSecure {| Challenge = challenge |}
        | { ClassId = 0x0Aus; MethodId = 0x1Eus } ->
            let! channelMax = readUInt16BE
            let! frameMax   = readUInt32BE
            let! heartbeat  = readUInt16BE

            return ConnectionTune {| ChannelMax = channelMax; FrameMax = frameMax; Heartbeat = heartbeat |}
        | { ClassId = 0x0Aus; MethodId = 0x29us } ->
            return ConnectionOpenOk
        | _ -> return failwith "method not supported"
    }
    static member Serialize = function
        | ConnectionStartOk message ->
            MethodHeader.Serialize { ClassId = 0x0Aus; MethodId = 0x0Bus }
            >> writeFieldTable message.PeerProperties
            >> writeShortString message.Mechanism
            >> writeLongString message.Response
            >> writeShortString message.Locale
        | ConnectionSecureOk message ->
            MethodHeader.Serialize { ClassId = 0x0Aus; MethodId = 0x15us }
            >> writeLongString message.Response
        | ConnectionTuneOk message ->
            MethodHeader.Serialize { ClassId = 0x0Aus; MethodId = 0x1Fus }
            >> writeUInt16BE message.ChannelMax
            >> writeUInt32BE message.FrameMax
            >> writeUInt16BE message.Heartbeat
        | ConnectionOpen message ->
            MethodHeader.Serialize { ClassId = 0x0Aus; MethodId = 0x28us }
            >> writeShortString message.VirtualHost
            >> writeShortString String.Empty // Deprecated 'capabilities' field...
            >> writeBoolean false // Deprecated 'insist' field...
        | _ -> failwith "method not supported"
end

type FrameType =
    | Method = 1uy
    | Header = 2uy
    | Body = 3uy
    | Heartbeat = 8uy

type FrameContent =
    | Method of Method
    | ContentHeader of ContentHeader
    | ContentBody of ReadOnlyMemory<uint8>
    | HeartBeat

type Frame = { Channel: uint16; Content: FrameContent } with
    static member Terminator = 0xCEuy
    static member Deserialize = deserialize {
        let! frameType  = readUInt8
        let! channel    = readUInt16BE
        let! length     = readUInt32BE
        let! payload    = readBytes (uint16 length)
        let! terminator = readUInt8

        if terminator <> Frame.Terminator then
            return failwith "framing error"

        let content =
            match LanguagePrimitives.EnumOfValue frameType with
            | FrameType.Method    -> Method(Method.Deserialize payload |> snd)
            | FrameType.Header    -> ContentHeader(ContentHeader.Deserialize payload |> snd)
            | FrameType.Body      -> ContentBody(readBytes (uint16 length) payload |> snd)
            | FrameType.Heartbeat -> HeartBeat
            | _ -> failwith "unknown frame type"

        return { Channel = channel; Content = content }
    }
    static member Type = function
        | { Content = (Method _) }        -> FrameType.Method
        | { Content = (ContentHeader _) } -> FrameType.Header
        | { Content = (ContentBody _) }   -> FrameType.Body
        | { Content = (HeartBeat _) }     -> FrameType.Heartbeat
    static member Serialize frame =
        let contentBuffer = ArrayBufferWriter<uint8>()
        match frame.Content with
        | Method method        -> Method.Serialize method contentBuffer
        | ContentHeader header -> ContentHeader.Serialize header contentBuffer
        | ContentBody body     -> writeBytes body contentBuffer
        | HeartBeat _          -> contentBuffer
        |> ignore

        writeUInt8 (uint8 (Frame.Type frame))
        >> writeUInt16BE frame.Channel
        >> writeUInt32BE (uint32 contentBuffer.WrittenMemory.Length)
        >> writeBytes contentBuffer.WrittenMemory
        >> writeUInt8 Frame.Terminator
end
