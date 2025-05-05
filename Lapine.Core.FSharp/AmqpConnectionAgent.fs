/// AmqpConnectionAgent wraps and manages an underlying TcpConnectionAgent and exposes an interface in terms of AMQP
/// frames, rather than the byte-stream-centric interface of the tcp connection...
module AmqpConnectionAgent

open System.Net
open System.Threading
open AmqpTypes
open AgentMessages

type private Command =
    | Connect of
        EndPoint: IPEndPoint *
        CancellationToken: CancellationToken *
        ReplyChannel: AsyncReplyChannel<ConnectResult>
    | Disconnect
    | HandleConnectionEvent of ConnectionEvent
    | TransmitFrame of Frame
    | TransmitProtocolHeader of ProtocolHeader

module private Behaviour =
    open Amqp
    open Agent
    open TcpConnectionAgent

    let rec disconnected =
        fun context ->
            match context.Message with
            | Connect(endpoint, cancellationToken, replyChannel) ->
                let tcpConnectionAgent = TcpConnectionAgent()

                match tcpConnectionAgent.Connect endpoint cancellationToken with
                | ConnectionFailed _ as result ->
                    replyChannel.Reply result
                    Terminate
                | Connected(connectionEvents, _) as result ->
                    connectionEvents.Add(context.Self.Post << HandleConnectionEvent)
                    replyChannel.Reply result
                    Become(connected tcpConnectionAgent)
            | Disconnect -> Ok // Already disconnected, nothing to do here...
            | _ -> Unhandled

    and connected tcpConnectionAgent =
        fun context ->
            match context.Message with
            | Disconnect ->
                tcpConnectionAgent.Transmit(
                    Frame.serialize
                        { Channel = 0us
                          Content = Method(ConnectionClose(0us, "", { ClassId = 0us; MethodId = 0us })) }
                )

                tcpConnectionAgent.Disconnect()
                // We could terminate the agent here, or wait for a disconnected event to bubble up from the tcp
                // connection agent. We could perhaps even enter a 'disconnecting' state in order to wait for the
                // disconnected event...
                Ok
            | HandleConnectionEvent(Disconnected _) -> Terminate
            | TransmitFrame frame ->
                tcpConnectionAgent.Transmit(Frame.serialize frame)
                Ok
            | TransmitProtocolHeader protocolHeader ->
                tcpConnectionAgent.Transmit(ProtocolHeader.serialize protocolHeader)
                Ok
            | _ -> Unhandled

type AmqpConnectionAgent() =
    let agent = Agent.startNew Behaviour.disconnected

    member _.Connect endpoint cancellationToken =
        agent.PostAndReply(fun replyChannel -> Connect(endpoint, cancellationToken, replyChannel))

    member _.Transmit frame = agent.Post(TransmitFrame frame)

    member _.Transmit protocolHeader =
        agent.Post(TransmitProtocolHeader protocolHeader)

    member _.Disconnect() = agent.Post Disconnect
