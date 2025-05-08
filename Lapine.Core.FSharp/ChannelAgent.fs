module ChannelAgent

open AmqpTypes
open Agent
open AgentMessages

type private Command =
    | Open of AsyncReplyChannel<OpenResponse>
    | Transmit of FrameContent
    | HandleFrame of FrameContent
    | Close of AsyncReplyChannel<CloseResponse>

module private Behaviour =
    let rec closed channelId (connectionSupervisor: ConnectionSupervisor.ConnectionSupervisor) frameEvents =
        fun context ->
            match context.Message with
            | Open replyChannel ->
                frameEvents
                |> Event.filter (fun { Channel = channel } -> channel = channelId)
                |> Event.add (fun { Content = content } -> context.Self.Post(HandleFrame content))

                connectionSupervisor.Transmit
                    { Channel = channelId
                      Content = Method ChannelOpen }

                Become(awaitingChannelOpenOK channelId connectionSupervisor replyChannel)
            | _ -> Unhandled

    and awaitingChannelOpenOK channelId amqpConnectionAgent replyChannel context =
        match context.Message with
        | HandleFrame(Method ChannelOpenOk) ->
            replyChannel.Reply OpenResponse.Opened
            Become(open' channelId amqpConnectionAgent)
        | _ -> Unhandled

    and open' channelId amqpConnectionAgent =
        fun context ->
            match context.Message with
            | Close replyChannel ->
                amqpConnectionAgent.Transmit
                    { Channel = channelId
                      Content = Method(ChannelClose(0us, "", { ClassId = 0us; MethodId = 0us })) }

                replyChannel.Reply Closed
                Become(awaitingChannelCloseOk replyChannel)
            | Transmit content ->
                amqpConnectionAgent.Transmit
                    { Channel = channelId
                      Content = content }

                Ok
            | _ -> Unhandled

    and awaitingChannelCloseOk replyChannel =
        fun context ->
            match context.Message with
            | HandleFrame(Method ChannelCloseOk) ->
                replyChannel.Reply Closed
                Terminate
            | _ -> Unhandled

open Behaviour

type ChannelAgent
    (channelId: uint16, connectionSupervisor: ConnectionSupervisor.ConnectionSupervisor, frameEvents: IEvent<Frame>) =
    let agent = Agent.startNew (closed channelId connectionSupervisor frameEvents)

    member _.Open() = agent.PostAndReply Open

    member _.Transmit content = agent.Post(Transmit(Method content))

    member _.Close() = agent.PostAndReply Close
