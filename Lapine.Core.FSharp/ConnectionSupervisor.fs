namespace Lapine.AmqpClient

open System.Net
open System.Threading
open AgentMessages

type private ConnectionSupervisorProtocol =
    | Connect of
        EndPoint: IPEndPoint *
        CancellationToken: CancellationToken *
        ReplyChannel: AsyncReplyChannel<ConnectResult>
    | Disconnect
    | Transmit of Frame

module private ConnectionSupervisorBehaviour =
    let rec disconnected connectionConfiguration =
        fun context ->
            match context.Message with
            | Connect(endpoint, cancellationToken, replyChanel) ->
                // Create a linked cancellation token that will cancel when either the client provided token is
                // cancelled, *or* when the configured connection timeout elapses...
                let cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                cts.CancelAfter connectionConfiguration.ConnectTimeout

                let amqpConnectionAgent = AmqpConnectionAgent()

                match amqpConnectionAgent.Connect endpoint cts.Token with
                | ConnectionFailed _ as result ->
                    replyChanel.Reply result
                    Terminate
                | Connected(connectionEvents, frameStream) ->
                    // Spawn a handshake agent to negotiate the connection...
                    let handshakeAgent =
                        HandshakeAgent(amqpConnectionAgent, frameStream, connectionEvents)

                    match handshakeAgent.NegotiateConnection(connectionConfiguration, cancellationToken) with
                    // The handshake process failed...
                    | NegotiationOutcome.NegotiationFailed reason ->
                        amqpConnectionAgent.Disconnect()
                        replyChanel.Reply(ConnectionFailed(ConnectionFailureReason.NegotiationFailed reason))
                        Terminate
                    // The handshake process completed successfully...
                    | ConnectionAgreed connection ->
                        // Spawn a heartbeat agent to manage AMQP heartbeats...
                        let heartbeatAgent = HeartbeatAgent(amqpConnectionAgent, frameStream)
                        let remoteFlatlineEvents = heartbeatAgent.Start connection.HeartbeatFrequency

                        // In the event of a remote flatline, disconnect from the server...
                        remoteFlatlineEvents.Add(fun _ -> amqpConnectionAgent.Disconnect())

                        replyChanel.Reply(ConnectResult.Connected(connectionEvents, frameStream))

                        Become(connected amqpConnectionAgent connection)
            | Disconnect -> Ok // Already disconnected, nothing to do here...

    and connected amqpConnectionAgent connection =
        fun context ->
            match context.Message with
            | Disconnect ->
                amqpConnectionAgent.Disconnect()
                Terminate
            | Transmit frame ->
                amqpConnectionAgent.Transmit frame
                Ok
            | _ -> Unhandled

type internal ConnectionSupervisor(connectionConfiguration: ConnectionConfiguration) =
    let agent =
        Agent.startNew (ConnectionSupervisorBehaviour.disconnected connectionConfiguration)

    member _.Connect (endPoint: IPEndPoint) (cancellationToken: CancellationToken) =
        agent.PostAndReply(fun replyChannel -> Connect(endPoint, cancellationToken, replyChannel))

    member _.Disconnect() = agent.Post Disconnect

    member _.Transmit(frame: Frame) = agent.Post(Transmit frame)
