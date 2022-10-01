module HandshakeAgent

open System

open AmqpTypes
open Amqp
open Client
open ClientTypes
open EventStream
open SocketAgent

type private Context = {
    ConnectionConfiguration: ConnectionConfiguration
    SocketAgent            : SocketAgent
    Self                   : MailboxProcessor<EventMessage>
    Message                : EventMessage
    Behaviour              : Context->Async<Context>
}

let rec private awaitingConnection context = async {
    match context.Message with
    | Connected ->
        context.SocketAgent.Transmit ProtocolHeader.default'
        return { context with Behaviour = awaitingConnectionStart }
    | _ -> return context
}
and private awaitingConnectionStart context = async {
    match context.Message with
    | FrameReceived { Content = Method (ConnectionStart message) } when not (List.contains context.ConnectionConfiguration.Locale message.Locales) ->
        // TODO: fail handshake...
        return { context with Behaviour = awaitingConnection }
    | FrameReceived { Content = Method (ConnectionStart message) } when not (List.contains (AuthenticationStrategy.mechanism context.ConnectionConfiguration.AuthenticationStrategy) message.Mechanisms) ->
        // TODO: fail handshake...
        return { context with Behaviour = awaitingConnection }
    | FrameReceived { Channel = channel; Content = Method (ConnectionStart message) } ->
        let response = ConnectionStartOk {|
            Locale         = context.ConnectionConfiguration.Locale
            Mechanism      = context.ConnectionConfiguration.AuthenticationStrategy |> AuthenticationStrategy.mechanism
            PeerProperties = context.ConnectionConfiguration.PeerProperties |> PeerProperties.toMap
            Response       = context.ConnectionConfiguration.AuthenticationStrategy |> AuthenticationStrategy.authenticate 0uy String.Empty
        |}
        context.SocketAgent.Transmit { Channel = channel; Content = Method response }
        return { context with Behaviour = awaitingConnectionSecureOrTune 1uy }
    | _ -> return context
}
and private awaitingConnectionSecureOrTune stage context = async {
    match context.Message with
    | FrameReceived { Channel = channel; Content = Method (ConnectionClose message) } ->
        return { context with Behaviour = awaitingConnection }
    | FrameReceived { Channel = channel; Content = Method (ConnectionSecure message) } ->
        let response = ConnectionSecureOk {|
            Response = context.ConnectionConfiguration.AuthenticationStrategy |> AuthenticationStrategy.authenticate stage message.Challenge
        |}
        context.SocketAgent.Transmit { Channel = channel; Content = Method response }
        return { context with Behaviour = awaitingConnectionSecureOrTune (stage + 1uy) }
    | FrameReceived { Channel = channel; Content = Method (ConnectionTune message) } ->
        let response = ConnectionTuneOk {|
            ChannelMax = min message.ChannelMax context.ConnectionConfiguration.MaximumChannelCount
            FrameMax   = min message.FrameMax context.ConnectionConfiguration.MaximumFrameSize
            Heartbeat  = min message.Heartbeat context.ConnectionConfiguration.HeartbeatFrequency
        |}
        context.SocketAgent.Transmit { Channel = channel; Content = Method response }
        let connectionOpen = ConnectionOpen {|
            VirtualHost = "/"
        |}
        context.SocketAgent.Transmit { Channel = channel; Content = Method connectionOpen }
        return { context with Behaviour = awaitingConnectionOpenOk }
    | _ -> return context
}
and private awaitingConnectionOpenOk context = async {
    match context.Message with
    | FrameReceived { Channel = channel; Content = Method ConnectionOpenOk } ->
        // TODO: handshake succeeded...
        return { context with Behaviour = awaitingConnection }
    | _ -> return context
}

type HandshakeAgent(connectionConfiguration, socketAgent) = class
    let agent = MailboxProcessor<EventMessage>.Start (fun inbox ->
        let rec loop context = async {
            let! message = inbox.Receive()
            let! context = context.Behaviour { context with Message = message }
            return! loop context
        }

        let subscription =
            EventStream
            |> Observable.choose (function
                | Connected as frame -> Some frame
                | FrameReceived { Channel = 0us } as frame -> Some frame
                | _ -> None
            )
            |> Observable.subscribe inbox.Post

        loop {
            ConnectionConfiguration = connectionConfiguration
            SocketAgent             = socketAgent
            Self                    = inbox
            Message                 = ConnectionFailed (Exception())
            Behaviour               = awaitingConnection
        }
    )
end
