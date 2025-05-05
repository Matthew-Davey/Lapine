module Agent

type Agent<'TProtocol> = MailboxProcessor<'TProtocol>

type Context<'TProtocol> =
    { Message: 'TProtocol
      Self: Agent<'TProtocol> }

and Behaviour<'TProtocol> = Context<'TProtocol> -> AgentResult<'TProtocol>

and AgentResult<'TProtocol> =
    | Ok
    | Become of Behaviour<'TProtocol>
    | Terminate
    | Unhandled

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
