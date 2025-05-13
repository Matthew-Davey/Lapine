namespace Lapine.Agents;

using System.Net;
using Lapine.Protocol;

static partial class SocketAgent {
    class Wrapper(IAgent<Protocol> agent) : ISocketAgent {
        async Task<(IObservable<ConnectionEvent> ConnectionEvents, IObservable<RawFrame> ReceivedFrames)> ISocketAgent.ConnectAsync(IPEndPoint endpoint, CancellationToken cancellationToken) {
            return await agent.PostAndReplyAsync<(IObservable<ConnectionEvent> ConnectionEvents, IObservable<RawFrame> ReceivedFrames)>(replyChannel => new Connect(endpoint, replyChannel, cancellationToken));
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
