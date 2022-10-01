module Client

open System
open System.Net
open System.Runtime.InteropServices

open ClientTypes

module ClientCapabilities =
    let default' = {
        BasicNack                          = true
        PublisherConfirms                  = true
        AuthenticationFailureNotifications = true
    }
    let toMap clientCapabilities = Map.ofList [
        ("basic_nack", clientCapabilities.BasicNack)
        ("publisher_confirms", clientCapabilities.PublisherConfirms)
        ("authentication_failure_close", clientCapabilities.AuthenticationFailureNotifications)
    ]

module PeerProperties =
    let empty = {
        Product            = None
        Version            = None
        Platform           = None
        Copyright          = None
        Information        = None
        ClientProvidedName = None
        Capabilities       = ClientCapabilities.default'
    }
    let default' = {
        Product            = Some "Lapine"
        Version            = Some "0.1.0"
        Platform           = Some RuntimeInformation.OSDescription
        Copyright          = Some "© Lapine Contributors 2019-2022"
        Information        = Some "Licensed under the MIT License https://opensource.org/licenses/MIT"
        ClientProvidedName = Some "Lapine 0.1.0"
        Capabilities       = ClientCapabilities.default'
    }
    let toMap peerProperies = Map.ofList<string, obj> [
        ("product", match peerProperies.Product with | Some product -> product | None -> String.Empty)
        ("version", match peerProperies.Version with | Some version -> version | None -> String.Empty)
        ("platform", match peerProperies.Platform with | Some platform -> platform | None -> String.Empty)
        ("copyright", match peerProperies.Copyright with | Some copyright -> copyright | None -> String.Empty)
        ("information", match peerProperies.Information with | Some information -> information | None -> String.Empty)
        ("connection_name", match peerProperies.ClientProvidedName with | Some name -> name | None -> String.Empty)
        ("capabilities", ClientCapabilities.toMap peerProperies.Capabilities)
    ]

module AuthenticationStrategy =
    let mechanism = function
        | Plain _ -> "PLAIN"
    let authenticate (stage: uint8) (challenge: string) = function
        | Plain(username, password) -> $"\000{username}\000{password}"

module ConnectionConfiguration =
    let defaultPort = 5672
    let defaultConnectionTimeout = TimeSpan.FromSeconds 5
    let defaultCommandTimeout = TimeSpan.FromSeconds 5
    let defaultAuthenticationStrategy = Plain ("guest", "guest")
    let defaultLocale = "en_US"
    let defaultVirtualHost = "/"
    let defaultMaximumFrameSize = 131072u
    let defaultMaximumChannelCount = 2047us
    let defaultHeartbeatFrequency = 60us
    let default' = {
        Endpoint               = IPEndPoint(IPAddress.Loopback, defaultPort)
        ConnectionTimeout      = defaultConnectionTimeout
        CommandTimeout         = defaultCommandTimeout
        AuthenticationStrategy = defaultAuthenticationStrategy
        Locale                 = defaultLocale
        PeerProperties         = PeerProperties.default'
        VirtualHost            = defaultVirtualHost
        MaximumFrameSize       = defaultMaximumFrameSize
        MaximumChannelCount    = defaultMaximumChannelCount
        HeartbeatFrequency     = defaultHeartbeatFrequency
    }
