namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

interface IChannelAgent {
    Task Open(UInt16 channelId, IObservable<RawFrame> frameStream, IObservable<ConnectionEvent> connectionEvents, ISocketAgent socketAgent, CancellationToken cancellationToken = default);
    Task Close(CancellationToken cancellationToken = default);
    Task DeclareExchange(ExchangeDefinition definition, CancellationToken cancellationToken = default);
    Task DeleteExchange(String exchange, DeleteExchangeCondition condition, CancellationToken cancellationToken = default);
    Task DeclareQueue(QueueDefinition definition, CancellationToken cancellationToken = default);
    Task DeleteQueue(String queue, DeleteQueueCondition condition, CancellationToken cancellationToken = default);
    Task BindQueue(Binding binding, CancellationToken cancellationToken = default);
    Task UnbindQueue(Binding binding, CancellationToken cancellationToken = default);
    Task<UInt32> PurgeQueue(String queue, CancellationToken cancellationToken = default);
    Task Publish(String exchange, String routingKey, RoutingFlags routingFlags, (BasicProperties, ReadOnlyMemory<Byte>) message, CancellationToken cancellationToken = default);
    Task<(DeliveryInfo delivery, BasicProperties properties, ReadOnlyMemory<Byte> body)?> GetMessage(String queue, Acknowledgements acknowledgements, CancellationToken cancellationToken = default);
    Task Acknowledge(UInt64 deliveryTag, Boolean multiple);
    Task Reject(UInt64 deliveryTag, Boolean requeue);
    Task SetPrefetchLimit(UInt16 limit, Boolean global, CancellationToken cancellationToken = default);
    Task<String> Consume(String queue, ConsumerConfiguration consumerConfiguration, IReadOnlyDictionary<String, Object>? arguments, CancellationToken cancellationToken = default);
    Task EnablePublisherConfirms(CancellationToken cancellationToken = default);
}

static partial class ChannelAgent {
    static public IChannelAgent Create(UInt32 maxFrameSize) =>
        new Wrapper(Agent<Protocol>.StartNew(Closed(maxFrameSize)));
}
