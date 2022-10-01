namespace AmqpTypes

open System

type ProtocolVersion = { Major: byte; Minor: byte; Revision: byte }

type ProtocolHeader = { Protocol: uint32; ProtocolId: byte; Version: ProtocolVersion }

type MethodHeader = { ClassId: uint16; MethodId: uint16 }

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
}

type ContentHeader = { ClassId: uint16; BodySize: uint64; Properties: BasicProperties }

type Method =
    | ConnectionStart    of {| Version: {| Major: uint8; Minor: uint8  |}; ServerProperties: Map<string, obj>; Mechanisms: string list; Locales: string list |}
    | ConnectionStartOk  of {| PeerProperties: Map<string, obj>; Mechanism: string; Response: string; Locale: string |}
    | ConnectionSecure   of {| Challenge: string |}
    | ConnectionSecureOk of {| Response: string |}
    | ConnectionTune     of {| ChannelMax: uint16; FrameMax: uint32; Heartbeat: uint16 |}
    | ConnectionTuneOk   of {| ChannelMax: uint16; FrameMax: uint32; Heartbeat: uint16 |}
    | ConnectionOpen     of {| VirtualHost: string |}
    | ConnectionOpenOk
    | ConnectionClose    of {| ReplyCode: uint16; ReplyText: string; FailingMethod: MethodHeader |}

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

type Frame = { Channel: uint16; Content: FrameContent }