namespace Lapine.Agents;

using Lapine.Client;

interface IAmqpClientAgent {
    Task<Object> EstablishConnection(ConnectionConfiguration configuration, CancellationToken cancellationToken = default);
    Task<Object> OpenChannel(CancellationToken cancellationToken = default);
    Task<Object> Disconnect();
    Task Stop();
}

static partial class AmqpClientAgent {
    static public IAmqpClientAgent Create() =>
        new Wrapper(Agent<Protocol>.StartNew(initialBehaviour: Disconnected()));
}
