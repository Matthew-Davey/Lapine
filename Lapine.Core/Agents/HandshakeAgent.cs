namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

abstract record HandshakeResult;
record ConnectionAgreed(ConnectionAgreement Agreement) : HandshakeResult;
record HandshakeFailed(Exception Fault) : HandshakeResult;

interface IHandshakeAgent {
    Task<HandshakeResult> StartHandshake(ConnectionConfiguration connectionConfiguration);
}

static partial class HandshakeAgent {
    static public IHandshakeAgent Create(IObservable<RawFrame> receivedFrames, IObservable<Object> connectionEvents, IDispatcherAgent dispatcher, CancellationToken cancellationToken) =>
        new Wrapper(Agent<Protocol>.StartNew(Unstarted(receivedFrames, connectionEvents, dispatcher, cancellationToken)));
}
