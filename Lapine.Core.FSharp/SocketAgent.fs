module SocketAgent

open System
open System.Buffers
open System.Net
open System.Net.Sockets
open System.Threading.Tasks

open EventStream
open Amqp

type private Protocol =
    | Start
    | BeginConnect of IPEndPoint
    | EndConnect of IAsyncResult
    | Tune of int32
    | Transmit of ReadOnlyMemory<uint8>
    | BeginReceive
    | EndReceive of IAsyncResult

type private Context = {
    Self     : MailboxProcessor<Protocol>
    Message  : Protocol
    Behaviour: Context->Async<Context>
}

let rec private disconnected context = async {
    match context.Message with
    | BeginConnect endpoint ->
        let socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        socket.BeginConnect(
            remoteEP = endpoint,
            callback = (fun asyncResult -> context.Self.Post (EndConnect asyncResult)),
            state    = socket
        ) |> ignore
        return { context with Behaviour = connecting socket }
    | _ -> return context
}
and private connecting (socket: Socket) context = async {
    match context.Message with
        | EndConnect asyncResult ->
            try
                socket.EndConnect(asyncResult)
                publish Connected
                context.Self.Post BeginReceive
                return { context with Behaviour = connected socket }
            with
            | :? SocketException as fault ->
                publish (ConnectionFailed fault)
                return { context with Behaviour = disconnected }
        | _ -> return context
}
and private connected (socket: Socket) =
    let mutable (frameBuffer: uint8 array, tail) = (Array.zeroCreate 131072, 0)
    fun context -> async {
        match context.Message with
        | Tune maxFrameSize ->
            Array.Resize(ref frameBuffer, maxFrameSize)
            return context
        | Transmit payload when payload.Length <= socket.SendBufferSize ->
            socket.Send(payload.Span) |> ignore
            printfn $"transmitted %A{payload.Length} bytes"
            return context
        | Transmit payload when payload.Length > socket.SendBufferSize ->
            for i in 0..(payload.Length / socket.SendBufferSize) do
                socket.Send(payload.Slice(i * socket.SendBufferSize, socket.SendBufferSize).Span) |> ignore
            printfn $"transmitted %A{payload.Length} bytes"
            return context
        | BeginReceive ->
            socket.BeginReceive(
                buffer      = frameBuffer,
                offset      = tail,
                size        = min (frameBuffer.Length - tail) 1024,
                socketFlags = SocketFlags.None,
                state       = socket,
                callback    = fun asyncResult -> context.Self.Post (EndReceive asyncResult)
            ) |> ignore
            return context
        | EndReceive asyncResult ->
            let received = socket.EndReceive(asyncResult)
            if received > 0 then
                tail <- tail + received
                if tail > 0 then
                    let buffer, frame = Frame.Deserialize (ReadOnlyMemory.op_Implicit frameBuffer.[..tail])
                    //buffer.CopyTo(frameBuffer); tail <- buffer.Length
                    tail <- 0
                    //printfn $"consumed frame, %A{tail} bytes remaining in buffer"
                    publish (FrameReceived frame)
            context.Self.Post BeginReceive
            return context
        | _ -> return context
    }

type ConnectionFailedException(message: string, endpoint: IPEndPoint, inner: Exception) =
    inherit Exception(message, inner)
    member _.Endpoint with get() = endpoint

type SocketAgent() = class
    let agent = MailboxProcessor<Protocol>.Start (fun inbox ->
        let rec loop context = async {
            let! message = inbox.Receive()
            let! context = context.Behaviour { context with Message = message }
            return! loop context
        }
        loop { Self = inbox; Message = Start; Behaviour = disconnected }
    )
    member _.ConnectAsync (endpoint: IPEndPoint) = async {
        let tcs = TaskCompletionSource()
        use _ = EventStream |> Observable.subscribe (fun message ->
                match message with
                | Connected -> tcs.SetResult ()
                | ConnectionFailed fault -> tcs.SetException fault
                | _ -> ())
        agent.Post (BeginConnect endpoint)
        do! Async.AwaitTask tcs.Task
    }
    member _.Transmit (payload: ReadOnlyMemory<uint8>) =
        agent.Post (Transmit payload)

    member this.Transmit (header: ProtocolHeader) =
        let buffer = ArrayBufferWriter()
        ProtocolHeader.Serialize header buffer |> ignore
        this.Transmit buffer.WrittenMemory

    member this.Transmit (frame: Frame) =
        let buffer = ArrayBufferWriter()
        Frame.Serialize frame buffer |> ignore
        this.Transmit buffer.WrittenMemory
end
