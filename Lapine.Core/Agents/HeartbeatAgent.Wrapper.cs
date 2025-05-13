namespace Lapine.Agents;

using Lapine.Protocol;

static partial class HeartbeatAgent {
    class Wrapper(IAgent<Protocol> agent) : IHeartbeatAgent {
        async Task<IObservable<HeartbeatEvent>> IHeartbeatAgent.Start(IObservable<RawFrame> frameStream, IDispatcherAgent dispatcher, TimeSpan frequency) {
            return await agent.PostAndReplyAsync<IObservable<HeartbeatEvent>>(replyChannel => new StartHeartbeat(frameStream, dispatcher, frequency, replyChannel));
        }

        async Task IHeartbeatAgent.Stop() =>
            await agent.StopAsync();
    }
}
