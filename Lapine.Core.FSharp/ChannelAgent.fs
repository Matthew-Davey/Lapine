namespace Lapine.AmqpClient

open AgentMessages

type private ChannelAgentProtocol =
    | Open of AsyncReplyChannel<OpenResponse>
    | Transmit of FrameContent
    | HandleFrame of FrameContent
    | Close of AsyncReplyChannel<CloseResponse>

type private State =
    { ChannelId: uint16
      ConnectionSupervisor: ConnectionSupervisor
      FrameEvents: IEvent<Frame> }

module private ChannelAgentBehaviour =
    let rec closed state =
        fun context ->
            match context.Message with
            | Open replyChannel ->
                state.FrameEvents
                |> Event.filter (fun { Channel = channel } -> channel = state.ChannelId)
                |> Event.add (fun { Content = content } -> context.Self.Post(HandleFrame content))

                state.ConnectionSupervisor.Transmit
                    { Channel = state.ChannelId
                      Content = Method ChannelOpen }

                Become(awaitingChannelOpenOK state replyChannel)
            | _ -> Unhandled

    and awaitingChannelOpenOK state replyChannel context =
        match context.Message with
        | HandleFrame(Method ChannelOpenOk) ->
            replyChannel.Reply OpenResponse.Opened
            Become(open' state)
        | _ -> Unhandled

    and open' state =
        fun context ->
            match context.Message with
            | Close replyChannel ->
                state.ConnectionSupervisor.Transmit
                    { Channel = state.ChannelId
                      Content = Method(ChannelClose(0us, "", { ClassId = 0us; MethodId = 0us })) }

                replyChannel.Reply Closed
                Become(awaitingChannelCloseOk state replyChannel)
            | Transmit content ->
                state.ConnectionSupervisor.Transmit
                    { Channel = state.ChannelId
                      Content = content }

                Ok
            | Open replyChannel ->
                // Already open, nothing to do here...
                replyChannel.Reply Opened
                Ok
            | _ -> Unhandled

    and awaitingChannelCloseOk state replyChannel =
        fun context ->
            match context.Message with
            | HandleFrame(Method ChannelCloseOk) ->
                replyChannel.Reply Closed
                Become(closed state)
            | _ -> Unhandled

type internal ChannelAgent(channelId: uint16, connectionSupervisor: ConnectionSupervisor, frameEvents: IEvent<Frame>) =
    let agent =
        Agent.startNew (
            ChannelAgentBehaviour.closed
                { ChannelId = channelId
                  ConnectionSupervisor = connectionSupervisor
                  FrameEvents = frameEvents }
        )

    member _.Open() = agent.PostAndReply Open

    member _.Transmit content = agent.Post(Transmit(Method content))

    member _.Close() = agent.PostAndReply Close
