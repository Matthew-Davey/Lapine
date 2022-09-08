open System.Net

open EventStream
open Amqp
open SocketAgent
open HandshakeAgent

let socketAgent = SocketAgent()
let handshakeAgent = HandshakeAgent(socketAgent);

let subscription =
    EventStream
    |> Observable.subscribe (function
           | FrameReceived frame -> printfn $"FrameReceived: %A{frame}"
           | _ -> ()
       )

socketAgent.ConnectAsync(IPEndPoint(IPAddress.Parse("127.0.0.1"), 5672))
|> Async.RunSynchronously

Async.Sleep 1000
|> Async.RunSynchronously
