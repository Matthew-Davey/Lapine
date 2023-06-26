namespace Lapine.Client;

using System.ComponentModel;
using Lapine.Agents;
using Lapine.Protocol;

using static Lapine.Agents.ChannelAgent.Protocol;

public class Channel {
    readonly IAgent _agent;
    readonly ConnectionConfiguration _connectionConfiguration;

    Boolean _closed = false;

    internal Channel(IAgent agent, in ConnectionConfiguration connectionConfiguration) {
        _agent                   = agent ?? throw new ArgumentNullException(nameof(agent));
        _connectionConfiguration = connectionConfiguration;
    }

    public async ValueTask CloseAsync(CancellationToken cancellationToken = default) {
        if (_closed)
            return; // Channel is already closed, nothing to do here...

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        switch (await _agent.PostAndReplyAsync(new Close(cts.Token))) {
            case true: {
                _closed = true;
                break;
            }
            case Exception fault: {
                throw fault;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(CloseAsync)}' method.");
        }
    }

    public async ValueTask DeclareExchangeAsync(ExchangeDefinition definition, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        switch (await _agent.PostAndReplyAsync(new DeclareExchange(definition, cts.Token))) {
            case true: {
                return;
            }
            case Exception fault: {
                throw fault;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(DeclareExchangeAsync)}' method.");
        }
    }

    public async ValueTask DeleteExchangeAsync(String exchange, DeleteExchangeCondition condition = DeleteExchangeCondition.None, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        switch (await _agent.PostAndReplyAsync(new DeleteExchange(exchange, condition, cts.Token))) {
            case true: {
                return;
            }
            case Exception fault: {
                throw fault;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(DeleteExchangeAsync)}' method.");
        }
    }

    public async ValueTask DeclareQueueAsync(QueueDefinition definition, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        switch (await _agent.PostAndReplyAsync(new DeclareQueue(definition, cts.Token))) {
            case true: {
                return;
            }
            case Exception fault: {
                throw fault;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(DeclareQueueAsync)}' method.");
        }
    }

    public async ValueTask DeleteQueueAsync(String queue, DeleteQueueCondition condition = DeleteQueueCondition.None, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        switch (await _agent.PostAndReplyAsync(new DeleteQueue(queue, condition, cts.Token))) {
            case true: {
                return;
            }
            case Exception fault: {
                throw fault;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(DeleteQueueAsync)}' method.");
        }
    }

    public async ValueTask BindQueueAsync(Binding binding, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        switch (await _agent.PostAndReplyAsync(new BindQueue(binding, cts.Token))) {
            case true: {
                return;
            }
            case Exception fault: {
                throw fault;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(BindQueueAsync)}' method.");
        }
    }

    public async ValueTask UnbindQueueAsync(Binding binding, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        switch (await _agent.PostAndReplyAsync(new UnbindQueue(binding, cts.Token))) {
            case true: {
                return;
            }
            case Exception fault: {
                throw fault;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(UnbindQueueAsync)}' method.");
        }
    }

    public async ValueTask PurgeQueueAsync(String queue, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        switch (await _agent.PostAndReplyAsync(new PurgeQueue(queue, cts.Token))) {
            case true: {
                return;
            }
            case Exception fault: {
                throw fault;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(PurgeQueueAsync)}' method.");
        }
    }

    public async ValueTask PublishAsync(String exchange, String routingKey, (MessageProperties Properties, ReadOnlyMemory<Byte> Payload) message, RoutingFlags routingFlags = RoutingFlags.None, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        var command = new Publish(exchange, routingKey, routingFlags, (message.Properties.ToBasicProperties(), message.Payload), cts.Token);

        switch (await _agent.PostAndReplyAsync(command)) {
            case true: {
                return;
            }
            case Exception fault: {
                throw fault;
            }
            case var message2: throw new Exception($"Unexpected message '{message2.GetType().FullName}' in '{nameof(PublishAsync)}' method.");
        }
    }

    public async ValueTask<(DeliveryInfo Delivery, MessageProperties Properties, ReadOnlyMemory<Byte> Body)?> GetMessageAsync(String queue, Acknowledgements acknowledgements, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        switch (await _agent.PostAndReplyAsync(new GetMessage(queue, acknowledgements, cts.Token))) {
            case (DeliveryInfo delivery, BasicProperties properties, ReadOnlyMemory<Byte> body): {
                return (delivery, MessageProperties.FromBasicProperties(properties), body);
            }
            case Exception fault: {
                throw fault;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(GetMessageAsync)}' method.");
        }
    }

    public async ValueTask AcknowledgeAsync(UInt64 deliveryTag, Boolean multiple = false) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        switch (await _agent.PostAndReplyAsync(new Acknowledge(deliveryTag, multiple))) {
            case true: {
                return;
            }
            case Exception fault: {
                throw fault;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(AcknowledgeAsync)}' method.");
        }
    }

    public async ValueTask RejectAsync(UInt64 deliveryTag, Boolean requeue) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        switch (await _agent.PostAndReplyAsync(new Reject(deliveryTag, requeue))) {
            case true: {
                return;
            }
            case Exception fault: {
                throw fault;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(RejectAsync)}' method.");
        }
    }

    public async ValueTask SetPrefetchLimitAsync(UInt16 limit, PrefetchLimitScope scope = PrefetchLimitScope.Consumer, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        var command = new SetPrefetchLimit(limit, scope switch {
            PrefetchLimitScope.Consumer => false,
            PrefetchLimitScope.Channel  => true,
            _                           => throw new InvalidEnumArgumentException(nameof(scope), (Int32)scope, typeof(PrefetchLimitScope))
        }, cts.Token);

        switch (await _agent.PostAndReplyAsync(command)) {
            case true: {
                return;
            }
            case Exception fault: {
                throw fault;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(SetPrefetchLimitAsync)}' method.");
        }
    }

    public async ValueTask<String> ConsumeAsync(String queue, ConsumerConfiguration consumerConfiguration, IReadOnlyDictionary<String, Object>? arguments = null, CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        switch (await _agent.PostAndReplyAsync(new Consume(queue, consumerConfiguration, arguments, cts.Token))) {
            case String consumerTag: {
                return consumerTag;
            }
            case Exception fault: {
                throw fault;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(ConsumeAsync)}' method.");
        }
    }

    public async ValueTask EnablePublisherConfirms(CancellationToken cancellationToken = default) {
        if (_closed)
            throw new InvalidOperationException("Channel is closed");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionConfiguration.CommandTimeout);

        switch (await _agent.PostAndReplyAsync(new EnablePublisherConfirms(cts.Token))) {
            case true: {
                return;
            }
            case Exception fault: {
                throw fault;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(EnablePublisherConfirms)}' method.");
        }
    }
}
