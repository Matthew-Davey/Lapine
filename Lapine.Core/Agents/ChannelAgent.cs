namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

interface IChannelAgent {
    Task<Object> Open(UInt16 channelId, IObservable<RawFrame> frameStream, IObservable<Object> connectionEvents, ISocketAgent socketAgent, CancellationToken cancellationToken = default);
    Task<Object> Close(CancellationToken cancellationToken = default);
    Task<Object> DeclareExchange(ExchangeDefinition definition, CancellationToken cancellationToken = default);
    Task<Object> DeleteExchange(String exchange, DeleteExchangeCondition condition, CancellationToken cancellationToken = default);
    Task<Object> DeclareQueue(QueueDefinition definition, CancellationToken cancellationToken = default);
    Task<Object> DeleteQueue(String queue, DeleteQueueCondition condition, CancellationToken cancellationToken = default);
    Task<Object> BindQueue(Binding binding, CancellationToken cancellationToken = default);
    Task<Object> UnbindQueue(Binding binding, CancellationToken cancellationToken = default);
    Task<Object> PurgeQueue(String queue, CancellationToken cancellationToken = default);
    Task<Object> Publish(String exchange, String routingKey, RoutingFlags routingFlags, (BasicProperties, ReadOnlyMemory<Byte>) message, CancellationToken cancellationToken = default);
    Task<Object> GetMessage(String queue, Acknowledgements acknowledgements, CancellationToken cancellationToken = default);
    Task<Object> Acknowledge(UInt64 deliveryTag, Boolean multiple);
    Task<Object> Reject(UInt64 deliveryTag, Boolean requeue);
    Task<Object> SetPrefetchLimit(UInt16 limit, Boolean global, CancellationToken cancellationToken = default);
    Task<Object> Consume(String queue, ConsumerConfiguration consumerConfiguration, IReadOnlyDictionary<String, Object>? arguments, CancellationToken cancellationToken = default);
    Task<Object> EnablePublisherConfirms(CancellationToken cancellationToken = default);
}

static partial class ChannelAgent {
    static public IChannelAgent Create(UInt32 maxFrameSize) =>
        new Wrapper(Agent<Protocol>.StartNew(Closed(maxFrameSize)));
}
