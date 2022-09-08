module HandshakeAgent

open System

open Amqp
open EventStream
open SocketAgent

type private Context = {
    SocketAgent: SocketAgent
    Self       : MailboxProcessor<EventMessage>
    Message    : EventMessage
    Behaviour  : Context->Async<Context>
}

let rec private awaitingConnection context = async {
    match context.Message with
    | Connected ->
        context.SocketAgent.Transmit ProtocolHeader.Default
        return { context with Behaviour = awaitingConnectionStart }
    | _ -> return context
}
and private awaitingConnectionStart context = async {
    match context.Message with
    | FrameReceived { Channel = channel; Content = Method (ConnectionStart message) } ->
        let response = ConnectionStartOk {|
            Locale         = "en_US"
            Mechanism      = "PLAIN"
            PeerProperties = Map.empty
            Response       = "\000guest\000guest"
        |}
        context.SocketAgent.Transmit { Channel = channel; Content = Method response }
        return { context with Behaviour = awaitingConnectionSecureOrTune }
    | _ -> return context
}
and private awaitingConnectionSecureOrTune context = async {
    match context.Message with
    | FrameReceived { Channel = channel; Content = Method (ConnectionSecure message) } ->
        let response = ConnectionSecureOk {|
            Response = "uhm"
        |}
        context.SocketAgent.Transmit { Channel = channel; Content = Method response }
        return context
    | FrameReceived { Channel = channel; Content = Method (ConnectionTune message) } ->
        let response = ConnectionTuneOk {|
            ChannelMax = message.ChannelMax
            FrameMax   = message.FrameMax
            Heartbeat  = message.Heartbeat
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
        return { context with Behaviour = awaitingConnection }
    | _ -> return context
}

type HandshakeAgent(socketAgent) = class
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

        loop { SocketAgent = socketAgent; Self = inbox; Message = ConnectionFailed (Exception()); Behaviour = awaitingConnection }
    )
end
