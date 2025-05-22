namespace Lapine.AmqpClient

open System.Threading
open AgentMessages

type private HeartbeatAgentProtocol =
    | Start of HeartbeatFrequency: uint16 * AsyncReplyChannel<IEvent<RemoteFlatline>>
    | Beat
    | ResetRemoteTimeout
    | TriggerRemoteFlatline
    | Stop

module private HeartbeatAgentBehaviour =
    let rec unstarted connectionAgent frameStream context =
        match context.Message with
        | Start(frequency, replyChannel) ->
            let heartbeatTimer = new Timer(fun _ -> context.Self.Post Beat)
            let remoteTimeoutTimer = new Timer(fun _ -> context.Self.Post TriggerRemoteFlatline)

            // Amy inbound frame (not just a heartbeat frame) will reset the remote flatline timer...
            frameStream |> Event.add (fun _ -> context.Self.Post ResetRemoteTimeout)

            let flatlineEvent = Event<RemoteFlatline>()
            replyChannel.Reply(flatlineEvent.Publish)

            Become(beating connectionAgent frequency heartbeatTimer remoteTimeoutTimer flatlineEvent)
        | _ -> Unhandled

    and beating (connectionAgent: AmqpConnectionAgent) frequency heartbeatTimer remoteTimeoutTimer flatlineEvent =
        heartbeatTimer.Change(dueTime = int32 frequency * 1000, period = int32 frequency * 1000)
        |> ignore

        // Remote flatline will be triggered if nothing is received from the server for 3 consecutive heartbeat periods...
        remoteTimeoutTimer.Change(dueTime = int frequency * 3000, period = Timeout.Infinite)
        |> ignore

        fun context ->
            match context.Message with
            | ResetRemoteTimeout ->
                printfn "Received frame from remote, resetting remote flatline timer..."

                remoteTimeoutTimer.Change(dueTime = int frequency * 3000, period = Timeout.Infinite)
                |> ignore

                Ok
            | TriggerRemoteFlatline ->
                flatlineEvent.Trigger RemoteFlatline
                heartbeatTimer.Dispose()
                remoteTimeoutTimer.Dispose()
                Terminate
            | Beat ->
                printfn "Sending heartbeat"
                connectionAgent.Transmit { Channel = 0us; Content = HeartBeat }
                Ok
            | Stop ->
                heartbeatTimer.Dispose()
                remoteTimeoutTimer.Dispose()
                Terminate
            | _ -> Unhandled

// Transmits heartbeat frames to the remote server periodically, and detects remote flatlines...
type private HeartbeatAgent(connectionAgent: AmqpConnectionAgent, frameStream: IEvent<Frame>) =
    let agent =
        Agent.startNew (HeartbeatAgentBehaviour.unstarted connectionAgent frameStream)

    member _.Start frequency =
        agent.PostAndReply(fun replyChannel -> Start(frequency, replyChannel))

    member _.Stop() = agent.Post Stop
