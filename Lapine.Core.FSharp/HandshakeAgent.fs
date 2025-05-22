namespace Lapine.AmqpClient

open System.Threading
open AgentMessages

type private HandshakeAgentProtocol =
    | NegotiateConnection of
        ConnectionConfiguration: ConnectionConfiguration *
        CancellationToken: CancellationToken *
        ReplyChannel: AsyncReplyChannel<NegotiationOutcome>
    | HandleFrame of FrameContent
    | Timeout
    | HandleConnectionEvent of ConnectionEvent

module private HandshakeAgentBehaviour =
    let rec unstarted (connectionAgent: AmqpConnectionAgent) frameStream connectionEvents =
        fun context ->
            match context.Message with
            | NegotiateConnection(connectionConfiguration, cancellationToken, replyChannel) ->
                // Cancellation triggers a timeout in the handshake process...
                cancellationToken.Register(fun () -> context.Self.Post Timeout) |> ignore

                // Subscribe to inbound method frames on channel zero...
                frameStream
                |> Event.filter (fun frame -> frame.Channel = 0us)
                |> Event.filter (fun frame -> Frame.type' frame = FrameType.Method)
                |> Event.add (fun frame -> context.Self.Post(HandleFrame frame.Content))

                // Subscribe to connection state events...
                connectionEvents |> Event.add (context.Self.Post << HandleConnectionEvent)

                // Start the handshake process by transmitting an AMQP protocol header to the remote server...
                connectionAgent.Transmit ProtocolHeader.Default

                // We now expect to see a ConnectionStart message from the remote server...
                Become(awaitingConnectionStart connectionConfiguration connectionAgent replyChannel)
            | _ -> Unhandled

    and awaitingConnectionStart connectionConfiguration (connectionAgent: AmqpConnectionAgent) replyChannel =
        let mechanism =
            AuthenticationStrategy.mechanism connectionConfiguration.AuthenticationStrategy

        let locale = connectionConfiguration.Locale

        fun context ->
            match context.Message with
            // Remote server does not support the authentication mechanism specified in connection configuration...
            | HandleFrame(Method(ConnectionStart(_, _, mechanisms, _))) when not (List.contains mechanism mechanisms) ->
                replyChannel.Reply(NegotiationFailed AuthenticationMechanismNotSupported)
                Terminate
            // Remote server does not support locale specified in connection configuration...
            | HandleFrame(Method(ConnectionStart(_, _, _, locales))) when not (List.contains locale locales) ->
                replyChannel.Reply(NegotiationFailed LocaleNotSupported)
                Terminate
            // We did not receive a ConnectionStart message from the remote server...
            | Timeout ->
                replyChannel.Reply(NegotiationFailed NegotiationTimedOut)
                Terminate
            // The connection disconnected whilst waiting for a ConnectionStart message from the remote server...
            | HandleConnectionEvent(Disconnected reason) ->
                replyChannel.Reply(NegotiationFailed(ConnectionInterrupted reason))
                Terminate
            // Received a ConnectionStart message from the remote server...
            | HandleFrame(Method(ConnectionStart(_, serverProperties, _, _))) ->
                // Build stage 0 authentication response...
                let authenticationResponse =
                    AuthenticationStrategy.authenticate 0uy null connectionConfiguration.AuthenticationStrategy

                let peerProperties = connectionConfiguration.PeerProperties

                let authenticationMechanism =
                    AuthenticationStrategy.mechanism connectionConfiguration.AuthenticationStrategy

                connectionAgent.Transmit
                    { Channel = 0us
                      Content =
                        Method(
                            ConnectionStartOk(
                                peerProperties,
                                authenticationMechanism,
                                authenticationResponse,
                                connectionConfiguration.Locale
                            )
                        ) }

                Become(
                    awaitingConnectionSecureOrTune
                        connectionConfiguration
                        connectionAgent
                        replyChannel
                        serverProperties
                        1uy
                )
            | _ -> Unhandled

    and awaitingConnectionSecureOrTune
        connectionConfiguration
        (connectionAgent: AmqpConnectionAgent)
        replyChannel
        serverProperties
        stage
        =
        fun context ->
            match context.Message with
            | Timeout ->
                replyChannel.Reply(NegotiationFailed NegotiationTimedOut)
                Terminate
            | HandleConnectionEvent(Disconnected reason) ->
                replyChannel.Reply(NegotiationFailed(ConnectionInterrupted reason))
                Terminate
            // Remote server is requesting another stage of authentication...
            | HandleFrame(Method(ConnectionSecure(challenge))) ->
                // Build stage x authentication response...
                let authenticationResponse =
                    AuthenticationStrategy.authenticate stage challenge connectionConfiguration.AuthenticationStrategy

                connectionAgent.Transmit
                    { Channel = 0us
                      Content = Method(ConnectionSecureOk authenticationResponse) }

                Become(
                    awaitingConnectionSecureOrTune
                        connectionConfiguration
                        connectionAgent
                        replyChannel
                        serverProperties
                        (stage + 1uy)
                )
            // Authentication completed successfully and the remote server is now asking to tune the connection...
            | HandleFrame(Method(ConnectionTune(channelMax, frameMax, heartbeatFrequency))) ->
                let heartbeatFrequency =
                    match connectionConfiguration.ConnectionIntegrityStrategy with
                    | ConnectionIntegrityStrategy.AmqpHeartbeats frequency ->
                        min heartbeatFrequency (uint16 frequency.TotalSeconds)
                    | _ -> heartbeatFrequency

                let maxFrameSize = min frameMax connectionConfiguration.MaximumFrameSize

                let maxChannelCount = min channelMax connectionConfiguration.MaximumChannelCount

                connectionAgent.Transmit
                    { Channel = 0us
                      Content = Method(ConnectionTuneOk(channelMax, frameMax, heartbeatFrequency)) }

                connectionAgent.Transmit
                    { Channel = 0us
                      Content = Method(ConnectionOpen connectionConfiguration.VirtualHost) }

                let connection =
                    { MaxChannelCount = maxChannelCount
                      MaxFrameSize = maxFrameSize
                      HeartbeatFrequency = heartbeatFrequency
                      ServerProperties = serverProperties }

                Become(awaitingChannelOpenOk connection replyChannel)
            | _ -> Unhandled

    and awaitingChannelOpenOk connection replyChannel =
        fun context ->
            match context.Message with
            | Timeout ->
                replyChannel.Reply(NegotiationFailed NegotiationTimedOut)
                Terminate
            | HandleConnectionEvent(Disconnected reason) ->
                replyChannel.Reply(NegotiationFailed(ConnectionInterrupted reason))
                Terminate
            | HandleFrame(Method(ConnectionOpenOk)) ->
                replyChannel.Reply(ConnectionAgreed connection)
                Terminate
            | _ -> Unhandled

/// Manages the process of negotiating a connection with a remote AMQP server
type private HandshakeAgent
    (connectionAgent: AmqpConnectionAgent, frameStream: IEvent<Frame>, connectionEvents: IEvent<ConnectionEvent>) =
    let agent =
        Agent.startNew (HandshakeAgentBehaviour.unstarted connectionAgent frameStream connectionEvents)

    member _.NegotiateConnection
        (
            connectionConfiguration: ConnectionConfiguration,
            cancellationToken: CancellationToken
        ) =
        agent.PostAndReply(fun replyChannel ->
            NegotiateConnection(connectionConfiguration, cancellationToken, replyChannel))
