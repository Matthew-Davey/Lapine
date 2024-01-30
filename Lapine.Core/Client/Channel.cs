namespace Lapine.Client;

using System.ComponentModel;
using Lapine.Agents;

public class Channel {
    readonly IChannelAgent _agent;
    readonly ConnectionConfiguration _connectionConfiguration;

    Boolean _closed = false;

    internal Channel(IChannelAgent agent, in ConnectionConfiguration connectionConfiguration) {
        _agent                   = agent ?? throw new ArgumentNullException(nameof(agent));
        _connectionConfiguration = connectionConfiguration;
    }

    public async ValueTask CloseAsync(CancellationToken cancellationToken = default) {
        if (_closed)
            return; // Channel is already closed, nothing to do here...

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        await _agent.Close(cts.Token);
        _closed = true;
    }

    public async ValueTask DeclareExchangeAsync(ExchangeDefinition definition, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        await _agent.DeclareExchange(definition, cts.Token);
    }

    public async ValueTask DeleteExchangeAsync(String exchange, DeleteExchangeCondition condition = DeleteExchangeCondition.None, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        await _agent.DeleteExchange(exchange, condition, cts.Token);
    }

    public async ValueTask DeclareQueueAsync(QueueDefinition definition, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        await _agent.DeclareQueue(definition, cts.Token);
    }

    public async ValueTask DeleteQueueAsync(String queue, DeleteQueueCondition condition = DeleteQueueCondition.None, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        await _agent.DeleteQueue(queue, condition, cts.Token);
    }

    public async ValueTask BindQueueAsync(Binding binding, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        await _agent.BindQueue(binding, cts.Token);
    }

    public async ValueTask UnbindQueueAsync(Binding binding, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        await _agent.UnbindQueue(binding, cts.Token);
    }

    public async ValueTask<UInt32> PurgeQueueAsync(String queue, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        return await _agent.PurgeQueue(queue, cts.Token);
    }

    public async ValueTask PublishAsync(String exchange, String routingKey, (MessageProperties Properties, ReadOnlyMemory<Byte> Payload) message, RoutingFlags routingFlags = RoutingFlags.None, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        await _agent.Publish(exchange, routingKey, routingFlags, (message.Properties.ToBasicProperties(), message.Payload), cts.Token);
    }

    public async ValueTask<(DeliveryInfo Delivery, MessageProperties Properties, ReadOnlyMemory<Byte> Body)?> GetMessageAsync(String queue, Acknowledgements acknowledgements, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        var message = await _agent.GetMessage(queue, acknowledgements, cts.Token);

        if (message.HasValue) {
            var (delivery, properties, body) = message.Value;
            return (delivery, MessageProperties.FromBasicProperties(properties), body);
        }
        else {
            return null;
        }
    }

    public async ValueTask AcknowledgeAsync(UInt64 deliveryTag, Boolean multiple = false) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        await _agent.Acknowledge(deliveryTag, multiple);
    }

    public async ValueTask RejectAsync(UInt64 deliveryTag, Boolean requeue) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        await _agent.Reject(deliveryTag, requeue);
    }

    public async ValueTask SetPrefetchLimitAsync(UInt16 limit, PrefetchLimitScope scope = PrefetchLimitScope.Consumer, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        await _agent.SetPrefetchLimit(limit, scope switch {
            PrefetchLimitScope.Consumer => false,
            PrefetchLimitScope.Channel  => true,
            _                           => throw new InvalidEnumArgumentException(nameof(scope), (Int32)scope, typeof(PrefetchLimitScope))
        }, cts.Token);
    }

    public async ValueTask<String> ConsumeAsync(String queue, ConsumerConfiguration consumerConfiguration, IReadOnlyDictionary<String, Object>? arguments = null, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        return await _agent.Consume(queue, consumerConfiguration, arguments, cts.Token);
    }

    public async ValueTask EnablePublisherConfirms(CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        await _agent.EnablePublisherConfirms(cts.Token);
    }
}
