namespace Lapine.AmqpClient

open System
open System.Buffers
open Buffer

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module private rec Serialize =
    let protocolVersion
        { Major = major
          Minor = minor
          Revision = revision }
        =
        writeUInt8 major >> writeUInt8 minor >> writeUInt8 revision

    let protocolHeader
        { Protocol = protocol
          ProtocolId = protocolId
          Version = version }
        =
        writeUInt32LE protocol
        >> writeUInt8 protocolId
        >> Serialize.protocolVersion version

    let methodHeader
        { ClassId = classId
          MethodId = methodId }
        =
        writeUInt16BE classId >> writeUInt16BE methodId

    let basicProperties (properties: BasicProperties) =
        writeUInt16BE (uint16 properties.PropertyFlags)
        >> (match properties.ContentType with
            | Some contentType -> writeShortString contentType
            | None -> id)
        >> (match properties.ContentEncoding with
            | Some encoding -> writeShortString encoding
            | None -> id)
        >> (match properties.Headers with
            | Some headers -> writeFieldTable headers
            | None -> id)
        >> (match properties.DeliveryMode with
            | Some mode -> writeUInt8 mode
            | None -> id)
        >> (match properties.Priority with
            | Some priority -> writeUInt8 priority
            | None -> id)
        >> (match properties.CorrelationId with
            | Some correlationId -> writeLongString correlationId
            | None -> id)
        >> (match properties.ReplyTo with
            | Some replyTo -> writeLongString replyTo
            | None -> id)
        >> (match properties.Expiration with
            | Some expiration -> writeLongString expiration
            | None -> id)
        >> (match properties.MessageId with
            | Some messageId -> writeLongString messageId
            | None -> id)
        >> (match properties.Timestamp with
            | Some timestamp -> writeUInt64BE timestamp
            | None -> id)
        >> (match properties.Type with
            | Some type' -> writeLongString type'
            | None -> id)
        >> (match properties.UserId with
            | Some userId -> writeLongString userId
            | None -> id)
        >> (match properties.AppId with
            | Some appId -> writeLongString appId
            | None -> id)
        >> (match properties.ClusterId with
            | Some clusterId -> writeLongString clusterId
            | None -> id)

    let contentHeader
        { ClassId = classId
          BodySize = bodySize
          Properties = properties }
        =
        writeUInt16BE classId
        >> writeUInt64BE bodySize
        >> Serialize.basicProperties properties

    let method =
        function
        | ConnectionStartOk(peerProperties, mechanism, response, locale) ->
            Serialize.methodHeader { ClassId = 0x0Aus; MethodId = 0x0Bus }
            >> writeFieldTable peerProperties.AsFieldTable
            >> writeShortString mechanism
            >> writeLongString response
            >> writeShortString locale
        | ConnectionSecureOk(response) ->
            Serialize.methodHeader { ClassId = 0x0Aus; MethodId = 0x15us }
            >> writeLongString response
        | ConnectionTuneOk(channelMax, frameMax, heartbeatFrequency) ->
            Serialize.methodHeader { ClassId = 0x0Aus; MethodId = 0x1Fus }
            >> writeUInt16BE channelMax
            >> writeUInt32BE frameMax
            >> writeUInt16BE heartbeatFrequency
        | ConnectionOpen(virtualHost) ->
            Serialize.methodHeader { ClassId = 0x0Aus; MethodId = 0x28us }
            >> writeShortString virtualHost
            >> writeShortString String.Empty // Deprecated 'capabilities' field...
            >> writeBoolean false // Deprecated 'insist' field...
        | ConnectionClose(replyCode, replyText, methodHeader) ->
            Serialize.methodHeader { ClassId = 0x0Aus; MethodId = 0x32us }
            >> writeUInt16BE replyCode
            >> writeShortString replyText
            >> writeUInt16BE methodHeader.ClassId
            >> writeUInt16BE methodHeader.MethodId
        | ChannelOpen ->
            Serialize.methodHeader { ClassId = 0x14us; MethodId = 0x0Aus }
            >> writeShortString String.Empty // reserved_1
        | ChannelClose(replyCode, replyText, methodHeader) ->
            Serialize.methodHeader { ClassId = 0x14us; MethodId = 0x28us  }
            >> writeUInt16BE replyCode
            >> writeShortString replyText
            >> writeUInt16BE methodHeader.ClassId
            >> writeUInt16BE methodHeader.MethodId

        // These messages are only ever received from the remote server, they should never need to be serialized...
        | ConnectionStart _ -> raise (NotSupportedException())
        | ConnectionSecure _ -> raise (NotSupportedException())
        | ConnectionTune _ -> raise (NotSupportedException())
        | ConnectionOpenOk -> raise (NotSupportedException())
        | ChannelOpenOk -> raise (NotSupportedException())
        | ChannelCloseOk -> raise (NotSupportedException())

    let frame (frame: Frame) =
        let contentBuffer = ArrayBufferWriter<uint8>()

        match frame.Content with
        | Method method -> Serialize.method method contentBuffer
        | ContentHeader header -> Serialize.contentHeader header contentBuffer
        | ContentBody body -> writeBytes body contentBuffer
        | HeartBeat -> contentBuffer
        |> ignore

        writeUInt8 (uint8 frame.Type)
        >> writeUInt16BE frame.Channel
        >> writeUInt32BE (uint32 contentBuffer.WrittenMemory.Length)
        >> writeBytes contentBuffer.WrittenMemory
        >> writeUInt8 Frame.Terminator

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module private rec Deserialize =
    let methodHeader =
        deserialize {
            let! classId = readUInt16BE
            let! methodId = readUInt16BE

            return
                { ClassId = classId
                  MethodId = methodId }
        }

    let basicProperties =
        deserialize {
            let! flags' = readUInt16BE
            let flags: PropertyFlags = LanguagePrimitives.EnumOfValue flags'

            let mutable properties = BasicProperties.None

            if flags.HasFlag PropertyFlags.ContentType then
                let! contentType = readShortString

                properties <-
                    { properties with
                        ContentType = Some contentType }

            if flags.HasFlag PropertyFlags.ContentEncoding then
                let! encoding = readShortString

                properties <-
                    { properties with
                        ContentEncoding = Some encoding }

            if flags.HasFlag PropertyFlags.Headers then
                let! headers = readFieldTable

                properties <-
                    { properties with
                        Headers = Some headers }

            if flags.HasFlag PropertyFlags.DeliveryMode then
                let! mode = readUInt8

                properties <-
                    { properties with
                        DeliveryMode = Some mode }

            if flags.HasFlag PropertyFlags.Priority then
                let! priority = readUInt8

                properties <-
                    { properties with
                        Priority = Some priority }

            if flags.HasFlag PropertyFlags.CorrelationId then
                let! correlationId = readLongString

                properties <-
                    { properties with
                        CorrelationId = Some correlationId }

            if flags.HasFlag PropertyFlags.ReplyTo then
                let! replyTo = readLongString

                properties <-
                    { properties with
                        ReplyTo = Some replyTo }

            if flags.HasFlag PropertyFlags.Expiration then
                let! expiration = readLongString

                properties <-
                    { properties with
                        Expiration = Some expiration }

            if flags.HasFlag PropertyFlags.MessageId then
                let! messageId = readLongString

                properties <-
                    { properties with
                        MessageId = Some messageId }

            if flags.HasFlag PropertyFlags.Timestamp then
                let! timestamp = readUInt64BE

                properties <-
                    { properties with
                        Timestamp = Some timestamp }

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

                properties <-
                    { properties with
                        ClusterId = Some clusterId }

            return properties
        }

    let contentHeader =
        deserialize {
            let! classId = readUInt16BE
            let! bodySize = readUInt64BE
            let! properties = Deserialize.basicProperties

            return
                { ClassId = classId
                  BodySize = bodySize
                  Properties = properties }
        }

    let method =
        deserialize {
            match! Deserialize.methodHeader with
            // ConnectionStart
            | { ClassId = 0x0Aus; MethodId = 0x0Aus } ->
                let! major = readUInt8
                let! minor = readUInt8
                let! serverProperties = readFieldTable
                let! mechanisms = readLongString
                let! locales = readLongString

                return
                    ConnectionStart(
                        Version = (major, minor),
                        ServerProperties = serverProperties,
                        Mechanisms = (mechanisms.Split(" ") |> List.ofArray),
                        Locales = (locales.Split(" ") |> List.ofArray)
                    )
            // ConnectionSecure
            | { ClassId = 0x0Aus; MethodId = 0x014us } ->
                let! challenge = readLongString

                return ConnectionSecure(Challenge = challenge)
            // ConnectionTune
            | { ClassId = 0x0Aus; MethodId = 0x1Eus } ->
                let! channelMax = readUInt16BE
                let! frameMax = readUInt32BE
                let! heartbeat = readUInt16BE

                return ConnectionTune(ChannelMax = channelMax, FrameMax = frameMax, Heartbeat = heartbeat)
            // ConnectionOpenOk
            | { ClassId = 0x0Aus; MethodId = 0x29us } -> return ConnectionOpenOk
            // ConnectionClose
            | { ClassId = 0x0Aus; MethodId = 0x32us } ->
                let! replyCode = readUInt16BE
                let! replyText = readShortString
                let! failingMethodHeader = Deserialize.methodHeader

                return
                    ConnectionClose(ReplyCode = replyCode, ReplyText = replyText, FailingMethod = failingMethodHeader)
            // ChannelOpenOk
            | { ClassId = 0x14us; MethodId = 0x0Bus } ->
                let! reserved1 = readLongString
                return ChannelOpenOk

            // ChannelCloseOk
            | { ClassId = 0x14us; MethodId = 0x29us } -> return ChannelCloseOk

            | _ -> return raise (NotImplementedException())
        }

    let frame =
        deserialize {
            let! frameType = readUInt8
            let! channel = readUInt16BE
            let! length = readUInt32BE
            let! payload = readBytes (uint16 length)
            let! terminator = readUInt8

            if terminator <> Frame.Terminator then
                return failwith "framing error"

            let content =
                match LanguagePrimitives.EnumOfValue frameType with
                | FrameType.Method -> Method(Deserialize.method payload |> snd)
                | FrameType.Header -> ContentHeader(Deserialize.contentHeader payload |> snd)
                | FrameType.Body -> ContentBody(readBytes (uint16 length) payload |> snd)
                | FrameType.Heartbeat -> HeartBeat
                | _ -> failwith "unknown frame type"

            return { Channel = channel; Content = content }
        }
