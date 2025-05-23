namespace Lapine.AmqpClient

open System
open System.Buffers
open System.Net
open System.Threading

open AgentMessages

type private TcpConnectionAgentProtocol =
    | Connect of
        Endpoint: IPEndPoint *
        CancellationToken: CancellationToken *
        ReplyChannel: AsyncReplyChannel<ConnectResult>
    | Tune of MaxFrameSize: uint32
    | Poll
    | PollCompleted of IAsyncResult
    | Transmit of (IBufferWriter<uint8> -> IBufferWriter<uint8>)
    | Disconnect

module private TcpConnectionAgentBehaviour =
    open System.Net.Sockets

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
            | :? AggregateException as fault ->
                fault
                    .Flatten()
                    .Handle(
                        function
                        | :? SocketException as fault when (fault.SocketErrorCode = SocketError.ConnectionRefused) ->
                            replyChannel.Reply(ConnectionFailed ConnectionFailureReason.ConnectionRefused)
                            true
                        | fault ->
                            replyChannel.Reply(ConnectionFailed(ConnectionFailureReason.Fault fault))
                            true
                    )

                Terminate
            | fault ->
                replyChannel.Reply(ConnectionFailed(ConnectionFailureReason.Fault fault))
                Terminate
        | _ -> Unhandled

    and connected socket connectionEvents frameEvents =
        let mutable rxBuffer, tail =
            (Array.zeroCreate (int ConnectionConfiguration.DefaultMaximumFrameSize), 0)

        let mutable txBuffer =
            ArrayBufferWriter<uint8>(initialCapacity = int ConnectionConfiguration.DefaultMaximumFrameSize)

        fun context ->
            match context.Message with
            | Tune maxFrameSize ->
                Array.Resize(&rxBuffer, int32 maxFrameSize)
                txBuffer <- ArrayBufferWriter<uint8>(initialCapacity = int maxFrameSize)
                Ok
            | Poll ->
                let mutable socketError = SocketError.Success

                socket.BeginReceive(
                    buffer = rxBuffer,
                    offset = tail,
                    size = Math.Min(4096, rxBuffer.Length - tail),
                    socketFlags = SocketFlags.None,
                    errorCode = &socketError,
                    state = socket,
                    callback = fun asyncResult -> context.Self.Post(PollCompleted asyncResult)
                )
                |> ignore

                match socketError with
                | SocketError.Success -> Ok
                | socketError ->
                    socket.Dispose()

                    connectionEvents.Trigger(
                        Disconnected(DisconnectReason.Fault(SocketException(errorCode = int socketError)))
                    )

                    Terminate
            | PollCompleted asyncResult ->
                let mutable socketError = SocketError.Success
                tail <- tail + socket.EndReceive(asyncResult, &socketError)

                match socketError with
                | SocketError.Success ->
                    if tail > 0 then
                        try
                            // TODO: Consider moving frameBuffer up to AmqpConnectionAgent...
                            let remaining, frame =
                                Deserialize.frame (ReadOnlyMemory.op_Implicit rxBuffer[..tail])

                            frameEvents.Trigger frame
                            remaining.CopyTo(rxBuffer)
                            tail <- remaining.Length - 1
                        with :? ArgumentOutOfRangeException ->
                            ()

                    context.Self.Post Poll
                    Ok
                | _ ->
                    socket.Disconnect(false)
                    socket.Dispose()
                    connectionEvents.Trigger(Disconnected(DisconnectReason.Fault(Exception())))
                    Terminate
            | Transmit serialize ->
                serialize txBuffer |> ignore
                let payload = txBuffer.WrittenMemory
                let mutable socketError = SocketError.Success

                socket.Send(payload.Span, SocketFlags.None, &socketError) |> ignore

                txBuffer.ResetWrittenCount()

                match socketError with
                | SocketError.Success -> Ok
                | SocketError.TimedOut ->
                    socket.Dispose()
                    connectionEvents.Trigger(Disconnected TimedOut)
                    Terminate
                | _ ->
                    socket.Dispose()
                    connectionEvents.Trigger(Disconnected RemoteDisconnected)
                    Terminate
            | Disconnect ->
                socket.Disconnect(true)
                socket.Dispose()
                connectionEvents.Trigger(Disconnected ClientTerminated)
                Terminate
            | _ -> Unhandled

type private TcpConnectionAgent() =
    let agent = Agent.startNew TcpConnectionAgentBehaviour.disconnected

    member _.Connect endpoint cancellationToken =
        agent.PostAndReply(fun replyChannel -> Connect(endpoint, cancellationToken, replyChannel))

    member _.Tune maxFrameSize = agent.Post(Tune maxFrameSize)

    member _.Transmit serializer = agent.Post(Transmit serializer)

    member _.Disconnect() = agent.Post Disconnect
