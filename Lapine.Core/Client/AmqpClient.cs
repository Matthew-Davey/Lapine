namespace Lapine.Client;

using Lapine.Agents;

public class AmqpClient(ConnectionConfiguration connectionConfiguration) : IAsyncDisposable {
    readonly IAmqpClientAgent _agent = AmqpClientAgent.Create();

    public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)  =>
        await _agent.EstablishConnection(connectionConfiguration, cancellationToken);

    public async ValueTask<Channel> OpenChannelAsync(CancellationToken cancellationToken = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(connectionConfiguration.CommandTimeout);

        var channelAgent = await _agent.OpenChannel(cts.Token);

        return new Channel(channelAgent, connectionConfiguration);
    }

    public async ValueTask DisposeAsync() {
        await _agent.Disconnect();
        await _agent.Stop();

        GC.SuppressFinalize(this);
    }
}
