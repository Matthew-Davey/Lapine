module Amqp

open System
open System.Buffers
open System.Text

open AmqpTypes
open Buffer

module ProtocolVersion =
    let default' = { Major = 0uy; Minor = 9uy; Revision = 1uy }
    let deserialize = deserialize {
        let! major = readUInt8
        let! minor = readUInt8
        let! revision = readUInt8
        
        return { Major = major; Minor = minor; Revision = revision }
    }
    let serialize { Major = major; Minor = minor; Revision = revision } =
        writeUInt8 major
        >> writeUInt8 minor
        >> writeUInt8 revision

module ProtocolHeader =
    let create protocol protocolId version =
        match String.length protocol with
        | 4 -> {
                   Protocol   = BitConverter.ToUInt32(Encoding.ASCII.GetBytes(protocol))
                   ProtocolId = protocolId
                   Version    = version
               }
        | _ -> failwith "value must be exactly four characters long"
    let default' = create "AMQP" 0uy ProtocolVersion.default'
    let serialize { Protocol = protocol; ProtocolId = protocolId; Version = version } =
        writeUInt32LE protocol
        >> writeUInt8 protocolId
        >> ProtocolVersion.serialize version

module MethodHeader =
    let deserialize = deserialize {
        let! classId  = readUInt16BE
        let! methodId = readUInt16BE

        return { ClassId = classId; MethodId = methodId }
    }
    let serialize { ClassId = classId; MethodId = methodId } =
        writeUInt16BE classId
        >> writeUInt16BE methodId

module BasicProperties =
    let none = {
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
    let flags properties =
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
    let serialize properties =
        writeUInt16BE (uint16 (flags properties))
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
    let deserialize = deserialize {
        let! flags' = readUInt16BE
        let flags: PropertyFlags = LanguagePrimitives.EnumOfValue flags'

        let mutable properties = none

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

module ContentHeader =
    let serialize { ClassId = classId; BodySize = bodySize; Properties = properties } =
        writeUInt16BE classId
        >> writeUInt64BE bodySize
        >> BasicProperties.serialize properties
    let deserialize = deserialize {
        let! classId    = readUInt16BE
        let! bodySize   = readUInt64BE
        let! properties = BasicProperties.deserialize

        return { ClassId = classId; BodySize = bodySize; Properties = properties }
    }

module Method =
    let deserialize = deserialize {
        match! MethodHeader.deserialize with
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
        | { ClassId = 0x0Aus; MethodId = 0x32us } ->
            let! replyCode = readUInt16BE
            let! replyText = readShortString
            let! failingMehodHeader = MethodHeader.deserialize
            
            return ConnectionClose {| ReplyCode = replyCode; ReplyText = replyText; FailingMethod = failingMehodHeader |}
         | _ -> return failwith "method not supported"
    }
    let serialize = function
        | ConnectionStartOk message ->
            MethodHeader.serialize { ClassId = 0x0Aus; MethodId = 0x0Bus }
            >> writeFieldTable message.PeerProperties
            >> writeShortString message.Mechanism
            >> writeLongString message.Response
            >> writeShortString message.Locale
        | ConnectionSecureOk message ->
            MethodHeader.serialize { ClassId = 0x0Aus; MethodId = 0x15us }
            >> writeLongString message.Response
        | ConnectionTuneOk message ->
            MethodHeader.serialize { ClassId = 0x0Aus; MethodId = 0x1Fus }
            >> writeUInt16BE message.ChannelMax
            >> writeUInt32BE message.FrameMax
            >> writeUInt16BE message.Heartbeat
        | ConnectionOpen message ->
            MethodHeader.serialize { ClassId = 0x0Aus; MethodId = 0x28us }
            >> writeShortString message.VirtualHost
            >> writeShortString String.Empty // Deprecated 'capabilities' field...
            >> writeBoolean false // Deprecated 'insist' field...
        | _ -> failwith "method not supported"

module Frame =
    let terminator = 0xCEuy
    let deserialize = deserialize {
        let! frameType  = readUInt8
        let! channel    = readUInt16BE
        let! length     = readUInt32BE
        let! payload    = readBytes (uint16 length)
        let! terminator = readUInt8

        if terminator <> terminator then
            return failwith "framing error"

        let content =
            match LanguagePrimitives.EnumOfValue frameType with
            | FrameType.Method    -> Method(Method.deserialize payload |> snd)
            | FrameType.Header    -> ContentHeader(ContentHeader.deserialize payload |> snd)
            | FrameType.Body      -> ContentBody(readBytes (uint16 length) payload |> snd)
            | FrameType.Heartbeat -> HeartBeat
            | _ -> failwith "unknown frame type"

        return { Channel = channel; Content = content }
    }
    let type' = function
        | { Content = (Method _) }        -> FrameType.Method
        | { Content = (ContentHeader _) } -> FrameType.Header
        | { Content = (ContentBody _) }   -> FrameType.Body
        | { Content = (HeartBeat _) }     -> FrameType.Heartbeat
    let serialize frame =
        let contentBuffer = ArrayBufferWriter<uint8>()
        match frame.Content with
        | Method method        -> Method.serialize method contentBuffer
        | ContentHeader header -> ContentHeader.serialize header contentBuffer
        | ContentBody body     -> writeBytes body contentBuffer
        | HeartBeat _          -> contentBuffer
        |> ignore

        writeUInt8 (uint8 (type' frame))
        >> writeUInt16BE frame.Channel
        >> writeUInt32BE (uint32 contentBuffer.WrittenMemory.Length)
        >> writeBytes contentBuffer.WrittenMemory
        >> writeUInt8 terminator
