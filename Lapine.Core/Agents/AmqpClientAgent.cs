namespace Lapine.Agents;

using Lapine.Client;

interface IAmqpClientAgent {
    Task EstablishConnection(ConnectionConfiguration configuration, CancellationToken cancellationToken = default);
    Task<IChannelAgent> OpenChannel(CancellationToken cancellationToken = default);
    Task Disconnect();
    Task Stop();
}

static partial class AmqpClientAgent {
    static public IAmqpClientAgent Create() =>
        new Wrapper(Agent<Protocol>.StartNew(initialBehaviour: Disconnected()));
}
