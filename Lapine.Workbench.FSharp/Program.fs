open System.Net

open ClientTypes
open Client
open EventStream
open SocketAgent
open HandshakeAgent

let connectionConfiguration = {
    ConnectionConfiguration.default' with
        AuthenticationStrategy = Plain ("guest", "guestr")
}

let socketAgent = SocketAgent()
let handshakeAgent = HandshakeAgent(connectionConfiguration, socketAgent);

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
