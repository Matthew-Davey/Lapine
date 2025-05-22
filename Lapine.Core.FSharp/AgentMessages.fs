namespace Lapine.AmqpClient

module AgentMessages =
    open System

    type DisconnectReason =
        | RemoteDisconnected
        | TimedOut
        | ClientTerminated
        | Fault of Exception

    type NegotiationFailureReason =
        | AuthenticationMechanismNotSupported
        | LocaleNotSupported
        | ConnectionInterrupted of DisconnectReason
        | NegotiationTimedOut

    type ConnectionEvent = Disconnected of DisconnectReason

    type ConnectionFailureReason =
        | NoEndpointSpecified
        | Timeout
        | ConnectionRefused
        | NegotiationFailed of NegotiationFailureReason
        | Fault of Exception

    type ConnectResult =
        | Connected of ConnectionEvents: IEvent<ConnectionEvent> * FrameStream: IEvent<Frame>
        | ConnectionFailed of ConnectionFailureReason

    type NegotiationOutcome =
        | NegotiationFailed of NegotiationFailureReason
        | ConnectionAgreed of AmqpConnection

    type RemoteFlatline = | RemoteFlatline
    type OpenResponse = | Opened
    type CloseResponse = | Closed
