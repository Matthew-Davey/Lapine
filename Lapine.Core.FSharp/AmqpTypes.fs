namespace Lapine.AmqpClient

open System
open System.Net

open type System.Text.Encoding

[<Struct>]
type ProtocolVersion =
    { Major: uint8
      Minor: uint8
      Revision: uint8 }

    static member Default =
        { Major = 0uy
          Minor = 9uy
          Revision = 1uy }

[<Struct>]
type ProtocolHeader =
    { Protocol: uint32
      ProtocolId: uint8
      Version: ProtocolVersion }

    static member Default =
        { Protocol = BitConverter.ToUInt32(ASCII.GetBytes("AMQP"))
          ProtocolId = 0uy
          Version = ProtocolVersion.Default }

[<Struct>]
type MethodHeader = { ClassId: uint16; MethodId: uint16 }

[<Flags>]
type PropertyFlags =
    | None = 0b0000000000000000us
    | ContentType = 0b1000000000000000us
    | ContentEncoding = 0b0100000000000000us
    | Headers = 0b0010000000000000us
    | DeliveryMode = 0b0001000000000000us
    | Priority = 0b0000100000000000us
    | CorrelationId = 0b0000010000000000us
    | ReplyTo = 0b0000001000000000us
    | Expiration = 0b0000000100000000us
    | MessageId = 0b0000000010000000us
    | Timestamp = 0b0000000001000000us
    | Type = 0b0000000000100000us
    | UserId = 0b0000000000010000us
    | AppId = 0b0000000000001000us
    | ClusterId = 0b0000000000000100us

type ClientCapabilities =
    { BasicNack: bool
      PublisherConfirms: bool }

    member this.AsFieldTable =
        Map
            [ ("BasicName", box this.BasicNack)
              ("PublisherConfirms", box this.PublisherConfirms) ]

type BasicProperties =
    { ContentType: string option
      ContentEncoding: string option
      Headers: Map<string, obj> option
      DeliveryMode: uint8 option
      Priority: uint8 option
      CorrelationId: string option
      ReplyTo: string option
      Expiration: string option
      MessageId: string option
      Timestamp: uint64 option
      Type: string option
      UserId: string option
      AppId: string option
      ClusterId: string option }

    static member None =
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

    member self.PropertyFlags =
        PropertyFlags.None
        ||| (match self.ContentType with
             | Some _ -> PropertyFlags.ContentType
             | None -> PropertyFlags.None)
        ||| (match self.ContentEncoding with
             | Some _ -> PropertyFlags.ContentEncoding
             | None -> PropertyFlags.None)
        ||| (match self.Headers with
             | Some _ -> PropertyFlags.Headers
             | None -> PropertyFlags.None)
        ||| (match self.DeliveryMode with
             | Some _ -> PropertyFlags.DeliveryMode
             | None -> PropertyFlags.None)
        ||| (match self.Priority with
             | Some _ -> PropertyFlags.Priority
             | None -> PropertyFlags.None)
        ||| (match self.CorrelationId with
             | Some _ -> PropertyFlags.CorrelationId
             | None -> PropertyFlags.None)
        ||| (match self.ReplyTo with
             | Some _ -> PropertyFlags.ReplyTo
             | None -> PropertyFlags.None)
        ||| (match self.Expiration with
             | Some _ -> PropertyFlags.Expiration
             | None -> PropertyFlags.None)
        ||| (match self.MessageId with
             | Some _ -> PropertyFlags.MessageId
             | None -> PropertyFlags.None)
        ||| (match self.Timestamp with
             | Some _ -> PropertyFlags.Timestamp
             | None -> PropertyFlags.None)
        ||| (match self.Type with
             | Some _ -> PropertyFlags.Type
             | None -> PropertyFlags.None)
        ||| (match self.UserId with
             | Some _ -> PropertyFlags.UserId
             | None -> PropertyFlags.None)
        ||| (match self.AppId with
             | Some _ -> PropertyFlags.AppId
             | None -> PropertyFlags.None)
        ||| (match self.ClusterId with
             | Some _ -> PropertyFlags.ClusterId
             | None -> PropertyFlags.None)

type PeerProperties =
    { Product: string option
      Version: string option
      Platform: string option
      Copyright: string option
      Information: string option
      ClientProvidedName: string option
      Capabilities: ClientCapabilities }

    static member Default =
        { Product = Some "Lapine"
          Version = Some "0.1.0"
          Platform = Some System.Runtime.InteropServices.RuntimeInformation.OSDescription
          Copyright = Some "© Lapine Contributors 2019-2025"
          Information = Some "Licensed under the MIT License https://opensource.org/licenses/MIT"
          ClientProvidedName = Some "Lapine 0.1.0"
          Capabilities =
            { BasicNack = true
              PublisherConfirms = true } }

    member self.AsFieldTable =
        Map.empty
        |> match self.Product with
           | Some product -> Map.add "Product" (box product)
           | None -> id
        |> match self.Version with
           | Some version -> Map.add "Version" (box version)
           | None -> id
        |> match self.Platform with
           | Some platform -> Map.add "Platform" (box platform)
           | None -> id
        |> match self.Copyright with
           | Some copyright -> Map.add "Copyright" (box copyright)
           | None -> id
        |> match self.Information with
           | Some information -> Map.add "Information" (box information)
           | None -> id
        |> match self.ClientProvidedName with
           | Some clientProvidedName -> Map.add "ClientProvidedName" (box clientProvidedName)
           | None -> id
        |> match Some self.Capabilities with
           | Some capabilities -> Map.add "Capabilities" (box capabilities.AsFieldTable)
           | None -> id

type ContentHeader =
    { ClassId: uint16
      BodySize: uint64
      Properties: BasicProperties }

type Method =
    | ConnectionStart of
        Version: (uint8 * uint8) *
        ServerProperties: Map<string, obj> *
        Mechanisms: string list *
        Locales: string list
    | ConnectionStartOk of PeerProperties: PeerProperties * Mechanism: string * Response: string * Locale: string
    | ConnectionSecure of Challenge: string
    | ConnectionSecureOk of Response: string
    | ConnectionTune of ChannelMax: uint16 * FrameMax: uint32 * Heartbeat: uint16
    | ConnectionTuneOk of ChannelMax: uint16 * FrameMax: uint32 * Heartbeat: uint16
    | ConnectionOpen of VirtualHost: string
    | ConnectionOpenOk
    | ConnectionClose of ReplyCode: uint16 * ReplyText: string * FailingMethod: MethodHeader
    | ChannelOpen
    | ChannelOpenOk
    | ChannelClose of ReplyCode: uint16 * ReplyText: string * FailingMethod: MethodHeader
    | ChannelCloseOk

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

type Frame =
    { Channel: uint16
      Content: FrameContent }

    static member Terminator = 0xCEuy

    member self.Type =
        match self with
        | { Content = (Method _) } -> FrameType.Method
        | { Content = (ContentHeader _) } -> FrameType.Header
        | { Content = (ContentBody _) } -> FrameType.Body
        | { Content = (HeartBeat) } -> FrameType.Heartbeat

type EndpointSelectionStrategy = | Random

type AuthenticationStrategy =
    | PlainText of Username: string * Password: string

    member self.Mechanism =
        match self with
        | PlainText _ -> "PLAIN"

    member self.Authenticate(stage: uint8, challenge: string) =
        match self with
        | PlainText(username, password) -> $"\000{username}\000{password}"

[<RequireQualifiedAccess>]
type ConnectionIntegrityStrategy =
    | None
    | AmqpHeartbeats of Frequency: TimeSpan
    | TcpKeepAlives

type ConnectionConfiguration =
    { EndPoints: IPEndPoint list
      EndPointSelectionStrategy: EndpointSelectionStrategy
      ConnectTimeout: TimeSpan
      AuthenticationStrategy: AuthenticationStrategy
      Locale: string
      PeerProperties: PeerProperties
      VirtualHost: string
      ConnectionIntegrityStrategy: ConnectionIntegrityStrategy
      MaximumFrameSize: uint32
      MaximumChannelCount: uint16 }

    static member DefaultPort = 5672
    static member DefaultEndpointSelectionStrategy = EndpointSelectionStrategy.Random
    static member DefaultConnectionTimeout = TimeSpan.FromSeconds(5L)

    static member DefaultAuthenticationStrategy =
        AuthenticationStrategy.PlainText("guest", "guest")

    static member DefaultLocale = "en_US"
    static member DefaultVirtualHost = "/"

    static member DefaultConnectionIntegrityStrategy =
        ConnectionIntegrityStrategy.AmqpHeartbeats <| TimeSpan.FromSeconds(60L)

    static member DefaultMaximumFrameSize = 131072u // From https://github.com/rabbitmq/rabbitmq-server/blob/7af37e5bb8bc4a517a6ab26a6038bef6cfa946e7/priv/schema/rabbit.schema#L564
    static member DefaultMaximumChannelCount = 2047us

    static member Default =
        { EndPoints = [ IPEndPoint(IPAddress.Loopback, ConnectionConfiguration.DefaultPort) ]
          EndPointSelectionStrategy = ConnectionConfiguration.DefaultEndpointSelectionStrategy
          ConnectTimeout = ConnectionConfiguration.DefaultConnectionTimeout
          AuthenticationStrategy = ConnectionConfiguration.DefaultAuthenticationStrategy
          Locale = ConnectionConfiguration.DefaultLocale
          PeerProperties = PeerProperties.Default
          VirtualHost = ConnectionConfiguration.DefaultVirtualHost
          ConnectionIntegrityStrategy = ConnectionConfiguration.DefaultConnectionIntegrityStrategy
          MaximumFrameSize = ConnectionConfiguration.DefaultMaximumFrameSize
          MaximumChannelCount = ConnectionConfiguration.DefaultMaximumChannelCount }

    member self.GetConnectionSequence() =
        match self.EndPointSelectionStrategy with
        | EndpointSelectionStrategy.Random -> self.EndPoints |> List.randomShuffle

type AmqpConnection =
    { MaxChannelCount: uint16
      MaxFrameSize: uint32
      HeartbeatFrequency: uint16
      ServerProperties: Map<string, obj> }
