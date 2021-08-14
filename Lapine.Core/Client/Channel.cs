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
        readonly ConnectionConfiguration _connectionConfiguration;

        Boolean _closed = false;

        internal Channel(ActorSystem system, PID agent, in ConnectionConfiguration connectionConfiguration) {
            _system                  = system ?? throw new ArgumentNullException(nameof(system));
            _agent                   = agent ?? throw new ArgumentNullException(nameof(agent));
            _connectionConfiguration = connectionConfiguration;
        }

        public async ValueTask CloseAsync(TimeSpan? timeout = null) {
            if (_closed)
                return; // Channel is already closed, nothing to do here...

            var command = new Close(timeout ?? _connectionConfiguration.CommandTimeout);
            _system.Root.Send(_agent, command);
            await command;

            _closed = true;
        }

        public async ValueTask DeclareExchangeAsync(ExchangeDefinition definition, TimeSpan? timeout = null) {
            if (_closed)
                throw new InvalidOperationException("Channel is closed.");

            var command = new DeclareExchange(definition, timeout ?? _connectionConfiguration.CommandTimeout);
            _system.Root.Send(_agent, command);
            await command;
        }

        public async ValueTask DeleteExchangeAsync(String exchange, DeleteExchangeCondition condition = DeleteExchangeCondition.None, TimeSpan? timeout = null) {
            if (_closed)
                throw new InvalidOperationException("Channel is closed.");

            var command = new DeleteExchange(exchange, condition, timeout ?? _connectionConfiguration.CommandTimeout);
            _system.Root.Send(_agent, command);
            await command;
        }

        public async ValueTask DeclareQueueAsync(QueueDefinition definition, TimeSpan? timeout = null) {
            if (_closed)
                throw new InvalidOperationException("Channel is closed.");

            var command = new DeclareQueue(definition, timeout ?? _connectionConfiguration.CommandTimeout);
            _system.Root.Send(_agent, command);
            await command;
        }

        public async ValueTask DeleteQueueAsync(String queue, DeleteQueueCondition condition = DeleteQueueCondition.None) {
            if (_closed)
                throw new InvalidOperationException("Channel is closed.");

            var command = new DeleteQueue(queue, condition);
            _system.Root.Send(_agent, command);
            await command;
        }

        public async ValueTask BindQueueAsync(String exchange, String queue, String routingKey = "#", IReadOnlyDictionary<String, Object>? arguments = null) {
            if (_closed)
                throw new InvalidOperationException("Channel is closed.");

            var command = new BindQueue(exchange, queue, routingKey, arguments ?? new Dictionary<String, Object>());
            _system.Root.Send(_agent, command);
            await command;
        }

        public async ValueTask UnbindQueueAsync(String exchange, String queue, String routingKey = "#", IReadOnlyDictionary<String, Object>? arguments = null) {
            if (_closed)
                throw new InvalidOperationException("Channel is closed.");

            var command = new UnbindQueue(exchange, queue, routingKey, arguments ?? new Dictionary<String, Object>());
            _system.Root.Send(_agent, command);
            await command;
        }

        public async ValueTask PurgeQueueAsync(String queue) {
            if (_closed)
                throw new InvalidOperationException("Channel is closed.");

            var command = new PurgeQueue(queue);
            _system.Root.Send(_agent, command);
            await command;
        }

        public async ValueTask PublishAsync(String exchange, String routingKey, (MessageProperties Properties, ReadOnlyMemory<Byte> Payload) message, RoutingFlags routingFlags = RoutingFlags.None) {
            if (_closed)
                throw new InvalidOperationException("Channel is closed.");

            var mandatory = routingFlags.HasFlag(RoutingFlags.Mandatory);
            var immediate = routingFlags.HasFlag(RoutingFlags.Immediate);
            var command   = new Publish(exchange, routingKey, (message.Properties.ToBasicProperties(), message.Payload), mandatory, immediate);
            _system.Root.Send(_agent, command);
            await command;
        }

        public async ValueTask<(DeliveryInfo Delivery, MessageProperties Properties, ReadOnlyMemory<Byte> Body)?> GetMessageAsync(String queue, Acknowledgements acknowledgements) {
            if (_closed)
                throw new InvalidOperationException("Channel is closed.");

            var command = new GetMessage(queue, acknowledgements);
            _system.Root.Send(_agent, command);
            return await command switch {
                null => null,
                (DeliveryInfo delivery, BasicProperties properties, ReadOnlyMemory<Byte> body) => (delivery, MessageProperties.FromBasicProperties(properties), body)
            };
        }

        public async ValueTask AcknowledgeAsync(UInt64 deliveryTag, Boolean multiple = false) {
            if (_closed)
                throw new InvalidOperationException("Channel is closed.");

            var command = new Acknowledge(deliveryTag, multiple);
            _system.Root.Send(_agent, command);
            await command;
        }

        public async ValueTask RejectAsync(UInt64 deliveryTag, Boolean requeue) {
            if (_closed)
                throw new InvalidOperationException("Channel is closed.");

            var command = new Reject(deliveryTag, requeue);
            _system.Root.Send(_agent, command);
            await command;
        }

        public async ValueTask SetPrefetchLimitAsync(UInt16 limit, PrefetchLimitScope scope = PrefetchLimitScope.Consumer) {
            if (_closed)
                throw new InvalidOperationException("Channel is closed.");

            var command = new SetPrefetchLimit(limit, scope switch {
                PrefetchLimitScope.Consumer => false,
                PrefetchLimitScope.Channel  => true,
                _                           => throw new InvalidEnumArgumentException(nameof(scope), (Int32)scope, typeof(PrefetchLimitScope))
            });
            _system.Root.Send(_agent, command);
            await command;
        }

        public async ValueTask<String> ConsumeAsync(String queue, ConsumerConfiguration consumerConfiguration, IReadOnlyDictionary<String, Object>? arguments = null) {
            if (_closed)
                throw new InvalidOperationException("Channel is closed.");

            var command = new Consume(queue, consumerConfiguration, arguments);
            _system.Root.Send(_agent, command);
            return await command;
        }
    }
}
