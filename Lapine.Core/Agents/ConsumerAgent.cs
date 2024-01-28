namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

interface IConsumerAgent {
    Task<Object> StartConsuming(String consumerTag, IObservable<RawFrame> frameStream, IDispatcherAgent dispatcherAgent, String queue, ConsumerConfiguration consumerConfiguration, IReadOnlyDictionary<String, Object>? arguments = null);
    Task HandlerReady(IMessageHandlerAgent handler);
}

static partial class ConsumerAgent {
    static public IConsumerAgent Create() =>
        new Wrapper(Agent<Protocol>.StartNew(Unstarted()));
}
