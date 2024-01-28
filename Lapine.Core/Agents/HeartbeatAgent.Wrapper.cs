namespace Lapine.Agents;

using Lapine.Protocol;

static partial class HeartbeatAgent {
    class Wrapper(IAgent<Protocol> agent) : IHeartbeatAgent {
        async Task<IObservable<HeartbeatEvent>> IHeartbeatAgent.Start(IObservable<RawFrame> frameStream, IDispatcherAgent dispatcher, TimeSpan frequency) {
            var reply = await agent.PostAndReplyAsync(replyChannel => new StartHeartbeat(frameStream, dispatcher, frequency, replyChannel));
            return (IObservable<HeartbeatEvent>) reply;
        }

        async Task IHeartbeatAgent.Stop() =>
            await agent.StopAsync();
    }
}
