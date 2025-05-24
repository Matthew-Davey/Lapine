namespace Lapine.AmqpClient

open System.Threading
open AgentMessages

type ConnectionResult =
    | Connected
    | InvalidConnectionConfiguration
    | ConnectionFailed of Map<string, ConnectResult>

type private AmqpClientProtocol =
    | EstablishConnection of
        ConnectionConfiguration: ConnectionConfiguration *
        CancellationToken: CancellationToken *
        ReplyChannel: AsyncReplyChannel<ConnectionResult>
    | OpenChannel of CancellationToken: CancellationToken * ReplyChannel: AsyncReplyChannel<ChannelAgent>
    | Disconnect

module private AmqpClientBehaviour =
    let rec private tryConnect connectionConfiguration cancellationToken failures endpoints =
        match endpoints with
        | [] -> Result.Error failures
        | endpoint :: remainingEndpoints ->
            let connectionSupervisor = ConnectionSupervisor(connectionConfiguration)

            match connectionSupervisor.Connect endpoint cancellationToken with
            | ConnectResult.ConnectionFailed _ as result ->
                tryConnect
                    connectionConfiguration
                    cancellationToken
                    (Map.add (endpoint.ToString()) result failures)
                    remainingEndpoints
            | ConnectResult.Connected(connectionEvents, frameStream) ->
                Result.Ok(connectionSupervisor, connectionEvents, frameStream)

    let rec disconnected =
        fun context ->
            match context.Message with
            | EstablishConnection(connectionConfiguration, cancellationToken, replyChannel) ->
                let cancellationTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)

                cancellationTokenSource.CancelAfter(connectionConfiguration.ConnectTimeout)

                let endpoints = connectionConfiguration.GetConnectionSequence()

                if endpoints.Length = 0 then
                    replyChannel.Reply InvalidConnectionConfiguration
                    Terminate
                else
                    match tryConnect connectionConfiguration cancellationToken Map.empty endpoints with
                    | Result.Error failures ->
                        replyChannel.Reply <| ConnectionFailed failures
                        Terminate
                    | Result.Ok(connectionSupervisor, connectionEvents, frameStream) ->
                        replyChannel.Reply Connected
                        Become(connected connectionSupervisor connectionEvents frameStream 1us)
            | _ -> Unhandled

    and connected connectionSupervisor connectionEvents frameStream nextChannelId =
        fun context ->
            match context.Message with
            | Disconnect ->
                connectionSupervisor.Disconnect()
                Become disconnected
            | OpenChannel(cancellationToken, replyChannel) ->
                // TODO: choose a channel number...
                let channelAgent = ChannelAgent(nextChannelId, connectionSupervisor, frameStream)

                match channelAgent.Open() with
                | Opened ->
                    replyChannel.Reply channelAgent
                    Become(connected connectionSupervisor connectionEvents frameStream (nextChannelId + 1us))
            | _ -> Unhandled

type public AmqpClient(connectionConfiguration: ConnectionConfiguration) =
    let agent = Agent.startNew AmqpClientBehaviour.disconnected

    member _.Connect(?cancellationToken0: CancellationToken) =
        let cancellationToken = defaultArg cancellationToken0 CancellationToken.None

        task {
            let result =
                agent.PostAndReply(fun replyChannel ->
                    EstablishConnection(connectionConfiguration, cancellationToken, replyChannel))

            return result
        }

    member _.Disconnect() = agent.Post Disconnect

    member _.OpenChannel(?cancellationToken0) =
        let cancellationToken = defaultArg cancellationToken0 CancellationToken.None

        task {
            let channelAgent =
                agent.PostAndReply(fun replyChannel -> OpenChannel(cancellationToken, replyChannel))

            return ChannelClient(channelAgent)
        }

and public ChannelClient internal (agent: ChannelAgent) =
    member _.Close() = agent.Close()
    member _.Open() = agent.Open()
