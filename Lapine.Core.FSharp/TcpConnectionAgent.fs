module TcpConnectionAgent

open System
open System.Buffers
open System.Net
open System.Net.Sockets
open System.Threading
open System.Threading.Tasks

open AmqpTypes
open Amqp
open Agent
open AgentMessages

type private Command =
    | Connect of
        Endpoint: IPEndPoint *
        CancellationToken: CancellationToken *
        ReplyChannel: AsyncReplyChannel<ConnectResult>
    | Tune of MaxFrameSize: uint32
    | Poll
    | PollCompleted of IAsyncResult
    | Transmit of (IBufferWriter<uint8> -> IBufferWriter<uint8>)
    | Disconnect

module private Behaviour =
    let rec disconnected context =
        match context.Message with
        | Connect(endpoint, cancellationToken, replyChannel) ->
            let socket =
                new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

            try
                socket.ConnectAsync(endpoint, cancellationToken).AsTask()
                |> Async.AwaitTask
                |> Async.RunSynchronously

                let frameEvents = Event<Frame>()
                let connectionEvents = Event<ConnectionEvent>()

                replyChannel.Reply(ConnectResult.Connected(connectionEvents.Publish, frameEvents.Publish))

                context.Self.Post Poll

                Become(connected socket connectionEvents frameEvents)
            with
            | :? OperationCanceledException ->
                replyChannel.Reply(ConnectionFailed ConnectionFailureReason.Timeout)
                Terminate
            | fault ->
                replyChannel.Reply(ConnectionFailed(ConnectionFailureReason.Fault fault))
                Terminate
        | _ -> Unhandled

    and connected socket connectionEvents frameEvents =
        let mutable frameBuffer, tail = (Array.zeroCreate 131072, 0)

        fun context ->
            match context.Message with
            | Tune maxFrameSize ->
                Array.Resize(ref frameBuffer, int32 maxFrameSize)
                Ok
            | Poll ->
                socket.BeginReceive(
                    buffer = frameBuffer,
                    offset = tail,
                    size = Math.Min(4096, frameBuffer.Length - tail),
                    socketFlags = SocketFlags.None,
                    state = socket,
                    callback = fun asyncResult -> context.Self.Post(PollCompleted asyncResult)
                )
                |> ignore

                Ok
            | PollCompleted asyncResult ->
                try
                    tail <- tail + socket.EndReceive(asyncResult)

                    if tail > 0 then
                        try
                            // TODO: Consider moving frameBuffer up to AmqpConnectionAgent...
                            let remaining, frame =
                                Frame.deserialize (ReadOnlyMemory.op_Implicit frameBuffer[..tail])

                            frameEvents.Trigger frame
                            remaining.CopyTo(frameBuffer)
                            tail <- remaining.Length - 1
                        with :? ArgumentOutOfRangeException ->
                            ()

                    context.Self.Post Poll
                    Ok
                with :? SocketException as fault when fault.ErrorCode = 104 ->
                    socket.Disconnect(true)
                    socket.Dispose()
                    connectionEvents.Trigger(Disconnected RemoteDisconnected)
                    Terminate
            | Transmit serialize ->
                let writer = ArrayBufferWriter<uint8>()
                serialize writer |> ignore
                let payload = writer.WrittenMemory

                try
                    socket.Send(payload.Span) |> ignore
                    Ok
                with :? SocketException ->
                    socket.Disconnect(true)
                    socket.Dispose()
                    connectionEvents.Trigger(Disconnected RemoteDisconnected)
                    Terminate
            | Disconnect ->
                socket.Disconnect(true)
                socket.Dispose()
                connectionEvents.Trigger(Disconnected ClientTerminated)
                Terminate
            | _ -> Unhandled

type TcpConnectionAgent() =
    let agent = Agent.startNew Behaviour.disconnected

    member _.Connect endpoint cancellationToken =
        agent.PostAndReply(fun replyChannel -> Connect(endpoint, cancellationToken, replyChannel))

    member _.Tune maxFrameSize = agent.Post(Tune maxFrameSize)

    member _.Transmit serializer = agent.Post(Transmit serializer)

    member _.Disconnect() = agent.Post Disconnect
