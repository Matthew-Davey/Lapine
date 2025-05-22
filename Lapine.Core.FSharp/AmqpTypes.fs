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

type PeerProperties =
    { Product: string option
      Version: string option
      Platform: string option
      Copyright: string option
      Information: string option
      ClientProvidedName: string option
      Capabilities: ClientCapabilities }

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

type EndpointSelectionStrategy = | Random

type AuthenticationStrategy = PlainText of Username: string * Password: string

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

type AmqpConnection =
    { MaxChannelCount: uint16
      MaxFrameSize: uint32
      HeartbeatFrequency: uint16
      ServerProperties: Map<string, obj> }
