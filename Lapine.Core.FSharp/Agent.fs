namespace Lapine.AmqpClient

type private Agent<'TProtocol> = MailboxProcessor<'TProtocol>

type private Context<'TProtocol> =
    { Message: 'TProtocol
      Self: Agent<'TProtocol> }

and private Behaviour<'TProtocol> = Context<'TProtocol> -> AgentResult<'TProtocol>

and private AgentResult<'TProtocol> =
    | Ok
    | Become of Behaviour<'TProtocol>
    | Terminate
    | Unhandled

[<RequireQualifiedAccess>]
module private Agent =
    let startNew<'TProtocol> (initialBehaviour: Behaviour<'TProtocol>) =
        MailboxProcessor<'TProtocol>.Start(fun inbox ->
            let rec messageLoop currentBehaviour =
                async {
                    let! message = inbox.Receive()

                    match currentBehaviour { Message = message; Self = inbox } with
                    | Ok -> return! messageLoop currentBehaviour
                    | Become newBehaviour -> return! messageLoop newBehaviour
                    | Terminate -> return ()
                    | Unhandled -> failwithf "message of type '%A' was unhandled" message
                }

            Event.add (printfn "%A") inbox.Error

            messageLoop initialBehaviour)
