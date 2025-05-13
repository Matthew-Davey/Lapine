namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

static partial class ChannelAgent {
    class Wrapper(IAgent<Protocol> agent) : IChannelAgent {
        async Task IChannelAgent.Open(UInt16 channelId, IObservable<RawFrame> frameStream, IObservable<ConnectionEvent> connectionEvents, ISocketAgent socketAgent, CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync(replyChannel => new Open(channelId, frameStream, connectionEvents, socketAgent, replyChannel, cancellationToken));

        async Task IChannelAgent.Close(CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync(replyChannel => new Close(replyChannel, cancellationToken));

        async Task IChannelAgent.DeclareExchange(ExchangeDefinition definition, CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync(replyChannel => new DeclareExchange(definition, replyChannel, cancellationToken));

        async Task IChannelAgent.DeleteExchange(String exchange, DeleteExchangeCondition condition, CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync(replyChannel => new DeleteExchange(exchange, condition, replyChannel, cancellationToken));

        async Task IChannelAgent.DeclareQueue(QueueDefinition definition, CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync(replyChannel => new DeclareQueue(definition, replyChannel, cancellationToken));

        async Task IChannelAgent.DeleteQueue(String queue, DeleteQueueCondition condition, CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync(replyChannel => new DeleteQueue(queue, condition, replyChannel, cancellationToken));

        async Task IChannelAgent.BindQueue(Binding binding, CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync(replyChannel => new BindQueue(binding, replyChannel, cancellationToken));

        async Task IChannelAgent.UnbindQueue(Binding binding, CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync(replyChannel => new UnbindQueue(binding, replyChannel, cancellationToken));

        async Task<UInt32> IChannelAgent.PurgeQueue(String queue, CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync<UInt32>(replyChannel => new PurgeQueue(queue, replyChannel, cancellationToken));

        async Task IChannelAgent.Publish(String exchange, String routingKey, RoutingFlags routingFlags, (BasicProperties, ReadOnlyMemory<Byte>) message, CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync(replyChannel => new Publish(exchange, routingKey, routingFlags, message, replyChannel, cancellationToken));

        async Task<(DeliveryInfo delivery, BasicProperties properties, ReadOnlyMemory<Byte> body)?> IChannelAgent.GetMessage(String queue, Acknowledgements acknowledgements, CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync<(DeliveryInfo delivery, BasicProperties properties, ReadOnlyMemory<Byte> body)?>(replyChannel => new GetMessage(queue, acknowledgements, replyChannel, cancellationToken));

        async Task IChannelAgent.Acknowledge(UInt64 deliveryTag, Boolean multiple) =>
            await agent.PostAndReplyAsync(replyChannel => new Acknowledge(deliveryTag, multiple, replyChannel));

        async Task IChannelAgent.Reject(UInt64 deliveryTag, Boolean requeue) =>
            await agent.PostAndReplyAsync(replyChannel => new Reject(deliveryTag, requeue, replyChannel));

        async Task IChannelAgent.SetPrefetchLimit(UInt16 limit, Boolean global, CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync(replyChannel => new SetPrefetchLimit(limit, global, replyChannel, cancellationToken));

        async Task<String> IChannelAgent.Consume(String queue, ConsumerConfiguration consumerConfiguration, IReadOnlyDictionary<String, Object>? arguments, CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync<String>(replyChannel => new Consume(queue, consumerConfiguration, arguments, replyChannel, cancellationToken));

        async Task IChannelAgent.EnablePublisherConfirms(CancellationToken cancellationToken) =>
            await agent.PostAndReplyAsync(replyChannel => new EnablePublisherConfirms(replyChannel, cancellationToken));
    }
}
