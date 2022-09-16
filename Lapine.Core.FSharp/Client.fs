module Client

open System
open System.Net
open System.Runtime.InteropServices

type ClientCapabilities = {
    BasicNack        : bool
    PublisherConfirms: bool
} with
    static member Default = {
        BasicNack         = true
        PublisherConfirms = true
    }
    member this.ToMap () = Map.ofList [
        ("basic_nack", this.BasicNack)
        ("publisher_confirms", this.PublisherConfirms)
    ]
end

type PeerProperties = {
    Product           : string option
    Version           : string option
    Platform          : string option
    Copyright         : string option
    Information       : string option
    ClientProvidedName: string option
    Capabilities      : ClientCapabilities
} with
    static member Empty = {
        Product            = None
        Version            = None
        Platform           = None
        Copyright          = None
        Information        = None
        ClientProvidedName = None
        Capabilities       = ClientCapabilities.Default
    }
    static member Default = {
        Product            = Some "Lapine"
        Version            = Some "0.1.0"
        Platform           = Some RuntimeInformation.OSDescription
        Copyright          = Some "© Lapine Contributors 2019-2022"
        Information        = Some "Licensed under the MIT License https://opensource.org/licenses/MIT"
        ClientProvidedName = Some "Lapine 0.1.0"
        Capabilities       = ClientCapabilities.Default
    }
    member this.ToMap () = Map.ofList<string, obj> [
        ("product", match this.Product with | Some product -> product | None -> String.Empty)
        ("version", match this.Version with | Some version -> version | None -> String.Empty)
        ("platform", match this.Platform with | Some platform -> platform | None -> String.Empty)
        ("copyright", match this.Copyright with | Some copyright -> copyright | None -> String.Empty)
        ("information", match this.Information with | Some information -> information | None -> String.Empty)
        ("connection_name", match this.ClientProvidedName with | Some name -> name | None -> String.Empty)
        ("capabilities", this.Capabilities.ToMap())
    ]
end

type AuthenticationStrategy =
    | Plain of string*string
with
    member this.Mechanism
        with get() =
            match this with
            | Plain _ -> "PLAIN"
    member this.Authenticate (stage: uint8) (challenge: string) =
        match this with
        | Plain(username, password) -> $"\000{username}\000{password}"
end

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
} with
    static member DefaultPort = 5672
    static member DefaultConnectionTimeout = TimeSpan.FromSeconds 5
    static member DefaultCommandTimeout = TimeSpan.FromSeconds 5
    static member DefaultAuthenticationStrategy = Plain ("guest", "guest")
    static member DefaultLocale = "en_US"
    static member DefaultVirtualHost = "/"
    static member DefaultMaximumFrameSize = 131072u
    static member DefaultMaximumChannelCount = 2047us
    static member DefaultHeartbeatFrequency = 60us
    static member Default = {
        Endpoint               = IPEndPoint(IPAddress.Loopback, ConnectionConfiguration.DefaultPort)
        ConnectionTimeout      = ConnectionConfiguration.DefaultConnectionTimeout
        CommandTimeout         = ConnectionConfiguration.DefaultCommandTimeout
        AuthenticationStrategy = ConnectionConfiguration.DefaultAuthenticationStrategy
        Locale                 = ConnectionConfiguration.DefaultLocale
        PeerProperties         = PeerProperties.Default
        VirtualHost            = ConnectionConfiguration.DefaultVirtualHost
        MaximumFrameSize       = ConnectionConfiguration.DefaultMaximumFrameSize
        MaximumChannelCount    = ConnectionConfiguration.DefaultMaximumChannelCount
        HeartbeatFrequency     = ConnectionConfiguration.DefaultHeartbeatFrequency
    }
    member this.Authenticate stage challenge =
        this.AuthenticationStrategy.Authenticate stage challenge
end
