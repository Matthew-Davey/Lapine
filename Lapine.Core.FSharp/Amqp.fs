namespace Lapine.AmqpClient

open Buffer

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module ProtocolVersion =
    let deserialize =
        deserialize {
            let! major = readUInt8
            let! minor = readUInt8
            let! revision = readUInt8

            return
                { Major = major
                  Minor = minor
                  Revision = revision }
        }

    let serialize
        { Major = major
          Minor = minor
          Revision = revision }
        =
        writeUInt8 major >> writeUInt8 minor >> writeUInt8 revision

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module ProtocolHeader =
    let serialize
        { Protocol = protocol
          ProtocolId = protocolId
          Version = version }
        =
        writeUInt32LE protocol
        >> writeUInt8 protocolId
        >> ProtocolVersion.serialize version

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module MethodHeader =
    let deserialize =
        deserialize {
            let! classId = readUInt16BE
            let! methodId = readUInt16BE

            return
                { ClassId = classId
                  MethodId = methodId }
        }

    let serialize
        { ClassId = classId
          MethodId = methodId }
        =
        writeUInt16BE classId >> writeUInt16BE methodId

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module ClientCapabilities =
    let toFieldTable capabilities =
        Map
            [ ("BasicNack", box capabilities.BasicNack)
              ("PublisherConfirms", box capabilities.PublisherConfirms) ]

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module BasicProperties =
    let none =
        { ContentType = None
          ContentEncoding = None
          Headers = None
          DeliveryMode = None
          Priority = None
          CorrelationId = None
          ReplyTo = None
          Expiration = None
          MessageId = None
          Timestamp = None
          Type = None
          UserId = None
          AppId = None
          ClusterId = None }

    let flags properties =
        PropertyFlags.None
        ||| (match properties.ContentType with
             | Some _ -> PropertyFlags.ContentType
             | None -> PropertyFlags.None)
        ||| (match properties.ContentEncoding with
             | Some _ -> PropertyFlags.ContentEncoding
             | None -> PropertyFlags.None)
        ||| (match properties.Headers with
             | Some _ -> PropertyFlags.Headers
             | None -> PropertyFlags.None)
        ||| (match properties.DeliveryMode with
             | Some _ -> PropertyFlags.DeliveryMode
             | None -> PropertyFlags.None)
        ||| (match properties.Priority with
             | Some _ -> PropertyFlags.Priority
             | None -> PropertyFlags.None)
        ||| (match properties.CorrelationId with
             | Some _ -> PropertyFlags.CorrelationId
             | None -> PropertyFlags.None)
        ||| (match properties.ReplyTo with
             | Some _ -> PropertyFlags.ReplyTo
             | None -> PropertyFlags.None)
        ||| (match properties.Expiration with
             | Some _ -> PropertyFlags.Expiration
             | None -> PropertyFlags.None)
        ||| (match properties.MessageId with
             | Some _ -> PropertyFlags.MessageId
             | None -> PropertyFlags.None)
        ||| (match properties.Timestamp with
             | Some _ -> PropertyFlags.Timestamp
             | None -> PropertyFlags.None)
        ||| (match properties.Type with
             | Some _ -> PropertyFlags.Type
             | None -> PropertyFlags.None)
        ||| (match properties.UserId with
             | Some _ -> PropertyFlags.UserId
             | None -> PropertyFlags.None)
        ||| (match properties.AppId with
             | Some _ -> PropertyFlags.AppId
             | None -> PropertyFlags.None)
        ||| (match properties.ClusterId with
             | Some _ -> PropertyFlags.ClusterId
             | None -> PropertyFlags.None)

    let serialize properties =
        writeUInt16BE (uint16 (flags properties))
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

    let deserialize =
        deserialize {
            let! flags' = readUInt16BE
            let flags: PropertyFlags = LanguagePrimitives.EnumOfValue flags'

            let mutable properties = none

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

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module PeerProperties =
    let toFieldTable peerProperties =
        Map.empty
        |> match peerProperties.Product with
           | Some product -> Map.add "Product" (box product)
           | None -> id
        |> match peerProperties.Version with
           | Some version -> Map.add "Version" (box version)
           | None -> id
        |> match peerProperties.Platform with
           | Some platform -> Map.add "Platform" (box platform)
           | None -> id
        |> match peerProperties.Copyright with
           | Some copyright -> Map.add "Copyright" (box copyright)
           | None -> id
        |> match peerProperties.Information with
           | Some information -> Map.add "Information" (box information)
           | None -> id
        |> match peerProperties.ClientProvidedName with
           | Some clientProvidedName -> Map.add "ClientProvidedName" (box clientProvidedName)
           | None -> id
        |> match Some peerProperties.Capabilities with
           | Some capabilities -> Map.add "Capabilities" (box (ClientCapabilities.toFieldTable capabilities))
           | None -> id

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module ContentHeader =
    let serialize
        { ClassId = classId
          BodySize = bodySize
          Properties = properties }
        =
        writeUInt16BE classId
        >> writeUInt64BE bodySize
        >> BasicProperties.serialize properties

    let deserialize =
        deserialize {
            let! classId = readUInt16BE
            let! bodySize = readUInt64BE
            let! properties = BasicProperties.deserialize

            return
                { ClassId = classId
                  BodySize = bodySize
                  Properties = properties }
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Method =
    open System

    let deserialize =
        deserialize {
            match! MethodHeader.deserialize with
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
                let! failingMethodHeader = MethodHeader.deserialize

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

    let serialize =
        function
        | ConnectionStartOk(peerProperties, mechanism, response, locale) ->
            MethodHeader.serialize { ClassId = 0x0Aus; MethodId = 0x0Bus }
            >> writeFieldTable (PeerProperties.toFieldTable peerProperties)
            >> writeShortString mechanism
            >> writeLongString response
            >> writeShortString locale
        | ConnectionSecureOk(response) ->
            MethodHeader.serialize { ClassId = 0x0Aus; MethodId = 0x15us }
            >> writeLongString response
        | ConnectionTuneOk(channelMax, frameMax, heartbeatFrequency) ->
            MethodHeader.serialize { ClassId = 0x0Aus; MethodId = 0x1Fus }
            >> writeUInt16BE channelMax
            >> writeUInt32BE frameMax
            >> writeUInt16BE heartbeatFrequency
        | ConnectionOpen(virtualHost) ->
            MethodHeader.serialize { ClassId = 0x0Aus; MethodId = 0x28us }
            >> writeShortString virtualHost
            >> writeShortString String.Empty // Deprecated 'capabilities' field...
            >> writeBoolean false // Deprecated 'insist' field...
        | ConnectionClose(replyCode, replyText, methodHeader) ->
            writeUInt16BE replyCode
            >> writeShortString replyText
            >> writeUInt16BE methodHeader.ClassId
            >> writeUInt16BE methodHeader.MethodId
        | ChannelOpen ->
            MethodHeader.serialize { ClassId = 0x14us; MethodId = 0x0Aus }
            >> writeShortString String.Empty // reserved_1
        | ChannelClose(replyCode, replyText, methodHeader) ->
            writeUInt16BE replyCode
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

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Frame =
    open System.Buffers

    let terminator = 0xCEuy

    let deserialize =
        deserialize {
            let! frameType = readUInt8
            let! channel = readUInt16BE
            let! length = readUInt32BE
            let! payload = readBytes (uint16 length)
            let! terminator = readUInt8

            if terminator <> terminator then
                return failwith "framing error"

            let content =
                match LanguagePrimitives.EnumOfValue frameType with
                | FrameType.Method -> Method(Method.deserialize payload |> snd)
                | FrameType.Header -> ContentHeader(ContentHeader.deserialize payload |> snd)
                | FrameType.Body -> ContentBody(readBytes (uint16 length) payload |> snd)
                | FrameType.Heartbeat -> HeartBeat
                | _ -> failwith "unknown frame type"

            return { Channel = channel; Content = content }
        }

    let type' =
        function
        | { Content = (Method _) } -> FrameType.Method
        | { Content = (ContentHeader _) } -> FrameType.Header
        | { Content = (ContentBody _) } -> FrameType.Body
        | { Content = (HeartBeat) } -> FrameType.Heartbeat

    let serialize frame =
        let contentBuffer = ArrayBufferWriter<uint8>()

        match frame.Content with
        | Method method -> Method.serialize method contentBuffer
        | ContentHeader header -> ContentHeader.serialize header contentBuffer
        | ContentBody body -> writeBytes body contentBuffer
        | HeartBeat -> contentBuffer
        |> ignore

        writeUInt8 (uint8 (type' frame))
        >> writeUInt16BE frame.Channel
        >> writeUInt32BE (uint32 contentBuffer.WrittenMemory.Length)
        >> writeBytes contentBuffer.WrittenMemory
        >> writeUInt8 terminator

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module ConnectionConfiguration =
    open System
    open System.Net

    let DefaultPort = 5672
    let DefaultEndpointSelectionStrategy = EndpointSelectionStrategy.Random
    let DefaultConnectionTimeout = TimeSpan.FromSeconds(5L)

    let DefaultAuthenticationStrategy =
        AuthenticationStrategy.PlainText("guest", "guest")

    let DefaultLocale = "en_US"

    let DefaultPeerProperties =
        { Product = Some "Lapine"
          Version = Some "0.1.0"
          Platform = Some System.Runtime.InteropServices.RuntimeInformation.OSDescription
          Copyright = Some "© Lapine Contributors 2019-2025"
          Information = Some "Licensed under the MIT License https://opensource.org/licenses/MIT"
          ClientProvidedName = Some "Lapine 0.1.0"
          Capabilities =
            { BasicNack = true
              PublisherConfirms = true } }

    let DefaultVirtualHost = "/"

    let DefaultConnectionIntegrityStrategy =
        ConnectionIntegrityStrategy.AmqpHeartbeats <| TimeSpan.FromSeconds(60L)

    let DefaultMaximumFrameSize = 131072u // From https://github.com/rabbitmq/rabbitmq-server/blob/7af37e5bb8bc4a517a6ab26a6038bef6cfa946e7/priv/schema/rabbit.schema#L564
    let DefaultMaximumChannelCount = 2047us

    let default' =
        { EndPoints = [ IPEndPoint(IPAddress.Loopback, DefaultPort) ]
          EndPointSelectionStrategy = DefaultEndpointSelectionStrategy
          ConnectTimeout = DefaultConnectionTimeout
          AuthenticationStrategy = DefaultAuthenticationStrategy
          Locale = DefaultLocale
          PeerProperties = DefaultPeerProperties
          VirtualHost = DefaultVirtualHost
          ConnectionIntegrityStrategy = DefaultConnectionIntegrityStrategy
          MaximumFrameSize = DefaultMaximumFrameSize
          MaximumChannelCount = DefaultMaximumChannelCount }

    let getConnectionSequence (connectionConfiguration: ConnectionConfiguration) =
        match connectionConfiguration.EndPointSelectionStrategy with
        | EndpointSelectionStrategy.Random -> connectionConfiguration.EndPoints |> List.randomShuffle

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module AuthenticationStrategy =
    let mechanism =
        function
        | PlainText _ -> "PLAIN"

    let authenticate (stage: uint8) (challenge: string) =
        function
        | PlainText(username, password) -> $"\000{username}\000{password}"
