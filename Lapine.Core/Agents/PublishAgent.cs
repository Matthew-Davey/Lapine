namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

interface IPublishAgent {
    Task Publish(String exchange, String routingKey, RoutingFlags routingFlags, (BasicProperties Properties, ReadOnlyMemory<Byte> Body) message);
}

static partial class PublishAgent {
    static public IPublishAgent Create(IObservable<RawFrame> receivedFrames, IDispatcherAgent dispatcher, UInt64 maxFrameSize, Boolean publisherConfirmsEnabled, UInt64 deliveryTag, CancellationToken cancellationToken) =>
        new Wrapper(Agent<Protocol>.StartNew(Unstarted(receivedFrames, dispatcher, maxFrameSize, publisherConfirmsEnabled, deliveryTag, cancellationToken)));
}
