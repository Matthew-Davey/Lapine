module AmqpClient

open System.Threading
open AmqpTypes
open Amqp
open Agent
open AgentMessages

type ConnectionResult =
    | Connected
    | InvalidConnectionConfiguration
    | ConnectionFailed of Map<string, ConnectResult>

type private Command =
    | EstablishConnection of
        ConnectionConfiguration: ConnectionConfiguration *
        CancellationToken: CancellationToken *
        ReplyChannel: AsyncReplyChannel<ConnectionResult>
    | OpenChannel of CancellationToken: CancellationToken
    | Disconnect

module private Behaviour =
    let rec private tryConnect connectionConfiguration cancellationToken failures endpoints =
        match endpoints with
        | [] -> Result.Error failures
        | endpoint :: remainingEndpoints ->
            let connectionSupervisor =
                ConnectionSupervisor.ConnectionSupervisor(connectionConfiguration)

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

                let endpoints =
                    ConnectionConfiguration.getConnectionSequence connectionConfiguration

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
                        Become(connected connectionSupervisor connectionEvents frameStream)
            | _ -> Unhandled

    and connected amqpConnectionAgent connectionEvents frameStream =
        fun context ->
            match context.Message with
            | _ -> Unhandled

type AmqpClient(connectionConfiguration: ConnectionConfiguration) =
    let agent = Agent.startNew Behaviour.disconnected

    member _.Connect(cancellationToken: CancellationToken) =
        task {
            let result =
                agent.PostAndReply(fun replyChannel ->
                    EstablishConnection(connectionConfiguration, cancellationToken, replyChannel))

            return result
        }
