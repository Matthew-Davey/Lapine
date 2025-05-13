namespace Lapine.Agents;

using Lapine.Client;

static partial class AmqpClientAgent {
    class Wrapper(IAgent<Protocol> agent) : IAmqpClientAgent {
        async Task IAmqpClientAgent.EstablishConnection(ConnectionConfiguration configuration, CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync(replyChannel => new EstablishConnection(configuration, replyChannel, cancellationToken));

        async Task<IChannelAgent> IAmqpClientAgent.OpenChannel(CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync<IChannelAgent>(replyChannel => new OpenChannel(replyChannel, cancellationToken));

        async Task IAmqpClientAgent.Disconnect() =>
            await agent.PostAndReplyAsync(replyChannel => new Disconnect(replyChannel));

        async Task IAmqpClientAgent.Stop() =>
            await agent.StopAsync();
    }
}
