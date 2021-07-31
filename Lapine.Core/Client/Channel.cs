namespace Lapine.Client {
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Lapine.Protocol;
    using Proto;

    using static Lapine.Agents.ChannelAgent.Protocol;

    public class Channel {
        readonly ActorSystem _system;
        readonly PID _agent;

        internal Channel(ActorSystem system, PID agent) {
            _system = system ?? throw new ArgumentNullException(nameof(system));
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        }

        public async ValueTask CloseAsync() {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new Close(promise));
            await promise.Task;
        }

        public async ValueTask DeclareExchangeAsync(ExchangeDefinition definition) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new DeclareExchange(definition, promise));
            await promise.Task;
        }

        public async ValueTask DeleteExchangeAsync(String exchange, DeleteExchangeCondition condition = DeleteExchangeCondition.None) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new DeleteExchange(exchange, condition, promise));
            await promise.Task;
        }

        public async ValueTask DeclareQueueAsync(QueueDefinition definition) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new DeclareQueue(definition, promise));
            await promise.Task;
        }

        public async ValueTask DeleteQueueAsync(String queue, DeleteQueueCondition condition = DeleteQueueCondition.None) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new DeleteQueue(queue, condition, promise));
            await promise.Task;
        }

        public async ValueTask BindQueueAsync(String exchange, String queue, String routingKey = "#", IReadOnlyDictionary<String, Object>? arguments = null) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new BindQueue(exchange, queue, routingKey, arguments ?? new Dictionary<String, Object>(), promise));
            await promise.Task;
        }

        public async ValueTask UnbindQueueAsync(String exchangem, String queue, String routingKey = "#", IReadOnlyDictionary<String, Object>? arguments = null) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new UnbindQueue(exchangem, queue, routingKey, arguments ?? new Dictionary<String, Object>(), promise));
            await promise.Task;
        }

        public async ValueTask PurgeQueueAsync(String queue) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new PurgeQueue(queue, promise));
            await promise.Task;
        }

        public async ValueTask PublishAsync(String exchange, String routingKey, (MessageProperties Properties, ReadOnlyMemory<Byte> Payload) message, RoutingFlags routingFlags = RoutingFlags.None) {
            var promise = new TaskCompletionSource();
            var mandatory = routingFlags.HasFlag(RoutingFlags.Mandatory);
            var immediate = routingFlags.HasFlag(RoutingFlags.Immediate);
            _system.Root.Send(_agent, new Publish(exchange, routingKey, (message.Properties.ToBasicProperties(), message.Payload), mandatory, immediate, promise));
            await promise.Task;
        }

        public async ValueTask<(DeliveryInfo Delivery, MessageProperties Properties, ReadOnlyMemory<Byte> Body)?> GetMessageAsync(String queue, Acknowledgements acknowledgements) {
            var promise = new TaskCompletionSource<(DeliveryInfo, BasicProperties, ReadOnlyMemory<Byte>)?>();
            _system.Root.Send(_agent, new GetMessage(queue, acknowledgements, promise));
            return await promise.Task switch {
                null => null,
                (DeliveryInfo delivery, BasicProperties properties, ReadOnlyMemory<Byte> body) => (delivery, MessageProperties.FromBasicProperties(properties), body)
            };
        }

        public async ValueTask AcknowledgeAsync(UInt64 deliveryTag, Boolean multiple = false) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new Acknowledge(deliveryTag, multiple, promise));
            await promise.Task;
        }

        public async ValueTask RejectAsync(UInt64 deliveryTag, Boolean requeue) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new Reject(deliveryTag, requeue, promise));
            await promise.Task;
        }

        public async ValueTask SetPrefetchLimitAsync(UInt16 limit, PrefetchLimitScope scope = PrefetchLimitScope.Consumer) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new SetPrefetchLimit(limit, scope switch {
                PrefetchLimitScope.Consumer => false,
                PrefetchLimitScope.Channel  => true,
                _                           => throw new InvalidEnumArgumentException(nameof(scope), (Int32)scope, typeof(PrefetchLimitScope))
            }, promise));
            await promise.Task;
        }

        public async ValueTask<String> ConsumeAsync(String queue, ConsumerConfiguration consumerConfiguration, IReadOnlyDictionary<String, Object>? arguments = null) {
            var promise = new TaskCompletionSource<String>();
            _system.Root.Send(_agent, new Consume(queue, consumerConfiguration, arguments, promise));
            return await promise.Task;
        }
    }
}
