namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

interface IHandshakeAgent {
    Task<ConnectionAgreement> StartHandshake(ConnectionConfiguration connectionConfiguration);
}

static partial class HandshakeAgent {
    static public IHandshakeAgent Create(IObservable<RawFrame> receivedFrames, IObservable<Object> connectionEvents, IDispatcherAgent dispatcher, CancellationToken cancellationToken) =>
        new Wrapper(Agent<Protocol>.StartNew(Unstarted(receivedFrames, connectionEvents, dispatcher, cancellationToken)));
}
