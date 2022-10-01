namespace ClientTypes

open System
open System.Net;

type ClientCapabilities = {
    BasicNack : bool
    PublisherConfirms: bool
    AuthenticationFailureNotifications: bool
}

type PeerProperties = {
    Product           : string option
    Version           : string option
    Platform          : string option
    Copyright         : string option
    Information       : string option
    ClientProvidedName: string option
    Capabilities      : ClientCapabilities
}

type AuthenticationStrategy =
    | Plain of string*string

type ConnectionConfiguration = {
    Endpoint              : IPEndPoint
    ConnectionTimeout     : TimeSpan
    CommandTimeout        : TimeSpan
    AuthenticationStrategy: AuthenticationStrategy
    Locale                : string
    PeerProperties        : PeerProperties
    VirtualHost           : string
    MaximumFrameSize      : uint32
    MaximumChannelCount   : uint16
    HeartbeatFrequency    : uint16
}