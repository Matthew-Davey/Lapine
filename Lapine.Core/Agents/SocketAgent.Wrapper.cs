namespace Lapine.Agents;

using System.Net;

static partial class SocketAgent {
    class Wrapper(IAgent<Protocol> agent) : ISocketAgent {
        async Task<ConnectResult> ISocketAgent.ConnectAsync(IPEndPoint endpoint, CancellationToken cancellationToken) {
            var reply = await agent.PostAndReplyAsync(replyChannel => new Connect(endpoint, replyChannel, cancellationToken));
            return (ConnectResult) reply;
        }

        async Task ISocketAgent.Tune(UInt32 maxFrameSize) =>
            await agent.PostAsync(new Tune(maxFrameSize));

        async Task ISocketAgent.EnableTcpKeepAlives(TimeSpan probeTime, TimeSpan retryInterval, Int32 retryCount) =>
            await agent.PostAsync(new EnableTcpKeepAlives(probeTime, retryInterval, retryCount));

        async Task ISocketAgent.Transmit(ISerializable entity) =>
            await agent.PostAsync(new Transmit(entity));

        async Task ISocketAgent.Disconnect() =>
            await agent.PostAsync(new Disconnect());

        async Task ISocketAgent.StopAsync() =>
            await agent.StopAsync();
    }
}
