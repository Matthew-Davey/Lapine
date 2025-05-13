namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

static partial class ConsumerAgent {
    class Wrapper(IAgent<Protocol> agent) : IConsumerAgent {
        async Task IConsumerAgent.StartConsuming(String consumerTag, IObservable<RawFrame> frameStream, IDispatcherAgent dispatcherAgent, String queue, ConsumerConfiguration consumerConfiguration, IReadOnlyDictionary<String, Object>? arguments) =>
            await agent.PostAndReplyAsync(replyChannel => new StartConsuming(this, consumerTag, frameStream, dispatcherAgent, queue, consumerConfiguration, arguments, replyChannel));

        async Task IConsumerAgent.HandlerReady(IMessageHandlerAgent handler) =>
            await agent.PostAsync(new HandlerReady(handler));
    }
}
