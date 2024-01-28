namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

static partial class ChannelAgent {
    abstract record Protocol;

    record Open(
        UInt16 ChannelId,
        IObservable<RawFrame> ReceivedFrames,
        IObservable<Object> ConnectionEvents,
        ISocketAgent SocketAgent,
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;

    record Close(
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;

    record DeclareExchange(
        ExchangeDefinition Definition,
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;

    record DeleteExchange(
        String Exchange,
        DeleteExchangeCondition Condition,
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;

    record DeclareQueue(
        QueueDefinition Definition,
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;

    record DeleteQueue(
        String Queue,
        DeleteQueueCondition Condition,
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;

    record BindQueue(
        Binding Binding,
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;

    record UnbindQueue(
        Binding Binding,
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;

    record PurgeQueue(
        String Queue,
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;

    record Publish(
        String Exchange,
        String RoutingKey,
        RoutingFlags RoutingFlags,
        (BasicProperties Properties, ReadOnlyMemory<Byte> Body) Message,
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken
    ) : Protocol;

    record GetMessage(
        String Queue,
        Acknowledgements Acknowledgements,
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;

    record Acknowledge(
        UInt64 DeliveryTag,
        Boolean Multiple,
        AsyncReplyChannel ReplyChannel
    ) : Protocol;

    record Reject(
        UInt64 DeliveryTag,
        Boolean Requeue,
        AsyncReplyChannel ReplyChannel
    ) : Protocol;

    record SetPrefetchLimit(
        UInt16 Limit,
        Boolean Global,
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;

    record Consume(
        String Queue,
        ConsumerConfiguration ConsumerConfiguration,
        IReadOnlyDictionary<String, Object>? Arguments,
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;

    record EnablePublisherConfirms(
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;
}
