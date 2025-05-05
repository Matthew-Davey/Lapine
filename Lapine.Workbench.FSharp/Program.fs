open System
open System.Net
open System.Threading

open AmqpTypes
open Amqp
open HandshakeAgent

let connectionConfiguration =
    { ConnectionConfiguration.default' with
        HeartbeatFrequency = 3us }

let cancellationTokenSource = new CancellationTokenSource()
cancellationTokenSource.CancelAfter(connectionConfiguration.ConnectionTimeout |> float |> TimeSpan.FromMilliseconds)

let connectionAgent = SocketAgent.create ()

match SocketAgent.connect (new IPEndPoint(IPAddress.Parse("10.0.0.0"), 5672)) cancellationTokenSource.Token connectionAgent with
| TcpConnectionAgent.ConnectionFailed fault ->
    printfn $"Failed to establish TCP connection to rabbitmq broker: %A{fault}"
| TcpConnectionAgent.ConnectionTimeout ->
    printfn "Failed to establish TCP connection to broker - timeout"
| TcpConnectionAgent.Connected(connectionEvents, frameEvents) ->
    printfn "Successfully established TCP connection to rabbitmq broker"

    let channelAgent = ChannelAgent.create 0us connectionAgent

    match ChannelAgent.``open`` channelAgent with
    | ChannelAgent.Opened -> printfn "channel 0 opened"

    let handshakeAgent =
        HandshakeAgent.create connectionAgent channelAgent frameEvents connectionEvents

    match
        HandshakeAgent.negotiateConnection connectionConfiguration cancellationTokenSource.Token handshakeAgent
    with
    | AuthenticationMechanismNotSupported -> printfn "authentication mechanism not supported"
    | LocaleNotSupported -> printfn "locale not supported"
    | RemoteDisconnected -> printfn "broker disconnected"
    | ConnectionTerminated -> printfn "connection terminated locally"
    | NegotiationTimedOut -> printfn "negotiation timed out"
    | ConnectionAgreed connection ->
        printfn "connection agreed %A" connection

        let heartbeatAgent = HeartbeatAgent.create channelAgent frameEvents

        let remoteFlatlineEvent =
            HeartbeatAgent.start (connection.HeartbeatFrequency) heartbeatAgent

        connectionEvents
        |> Event.add (fun x ->
            HeartbeatAgent.stop heartbeatAgent
            SocketAgent.disconnect connectionAgent
            printfn "%A" x)

        remoteFlatlineEvent |> Event.add (printfn "%A")

        Async.Sleep(60000) |> Async.RunSynchronously
        SocketAgent.disconnect connectionAgent
        Async.Sleep(100) |> Async.RunSynchronously
