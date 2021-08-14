namespace Lapine.Agents {
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading;
    using System.Threading.Tasks;
    using Lapine.Agents.Middleware;
    using Lapine.Client;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;
    using Proto.Timers;

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.DispatcherAgent.Protocol;
    using static Lapine.Agents.ChannelAgent.Protocol;
    using static Lapine.Agents.ConsumerAgent.Protocol;
    using static Lapine.Agents.SocketAgent.Protocol;

    static class ChannelAgent {
        static public class Protocol {
            public record Open(UInt16 ChannelId, PID TxD, TimeSpan Timeout) : AsyncCommand;
            public record Close(TimeSpan Timeout) : AsyncCommand;
            public record DeclareExchange(ExchangeDefinition Definition, TimeSpan Timeout) : AsyncCommand;
            public record DeleteExchange(
                String Exchange,
                DeleteExchangeCondition Condition,
                TimeSpan Timeout
            ) : AsyncCommand;
            public record DeclareQueue(QueueDefinition Definition, TimeSpan Timeout) : AsyncCommand;
            public record DeleteQueue(String Queue, DeleteQueueCondition Condition, TimeSpan Timeout) : AsyncCommand;
            public record BindQueue(Binding Binding, TimeSpan Timeout): AsyncCommand;
            public record UnbindQueue(Binding Binding, TimeSpan Timeout) : AsyncCommand;
            public record PurgeQueue(String Queue) : AsyncCommand;
            public record Publish(
                String Exchange,
                String RoutingKey,
                (BasicProperties Properties, ReadOnlyMemory<Byte> Body) Message,
                Boolean Mandatory,
                Boolean Immediate
            ) : AsyncCommand;
            public record GetMessage(
                String Queue,
                Acknowledgements Acknowledgements
            ) : AsyncCommand<(DeliveryInfo, BasicProperties, ReadOnlyMemory<Byte>)?>;
            public record Acknowledge(UInt64 DeliveryTag, Boolean Multiple) : AsyncCommand;
            public record Reject(UInt64 DeliveryTag, Boolean Requeue) : AsyncCommand;
            public record SetPrefetchLimit(UInt16 Limit, Boolean Global) : AsyncCommand;
            public record Consume(
                String Queue,
                ConsumerConfiguration ConsumerConfiguration,
                IReadOnlyDictionary<String, Object>? Arguments
            ) : AsyncCommand<String>;
        }

        static public Props Create(UInt32 maxFrameSize) =>
            Props.FromProducer(() => new Actor(maxFrameSize))
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames())
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundContentHeaderFrames())
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundContentBodyFrames());

        readonly record struct State(
            Guid SubscriptionId,
            UInt16 ChannelId,
            PID Dispatcher,
            IImmutableDictionary<String, PID> Consumers
        );

        class Actor : IActor {
            readonly UInt32 _maxFrameSize;
            readonly Behavior _behaviour;

            public Actor(UInt32 maxFrameSize) {
                _maxFrameSize = maxFrameSize;
                _behaviour    = new Behavior(Closed);
            }

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            async Task Closed(IContext context) {
                switch (context.Message) {
                    case Open open: {
                        var subscription = context.System.EventStream.Subscribe<FrameReceived>(
                            predicate: message => message.Frame.Channel == open.ChannelId,
                            action   : message => context.Send(context.Self!, message)
                        );
                        var state = new State(
                            SubscriptionId: subscription.Id,
                            ChannelId     : open.ChannelId,
                            Dispatcher    : context.SpawnNamed(
                                name : "dispatcher",
                                props: DispatcherAgent.Create()
                            ),
                            Consumers     : ImmutableDictionary<String, PID>.Empty
                        );
                        context.Send(state.Dispatcher, new DispatchTo(open.TxD, open.ChannelId));

                        var promise = new TaskCompletionSource();
                        context.Spawn(ChannelOpenActor.Create(state, open.Timeout, promise));

                        try {
                            await promise.Task;
                            open.SetResult();
                            _behaviour.Become(Open(state));
                        }
                        catch (Exception fault) {
                            open.SetException(fault);
                        }
                        break;
                    }
                }
            }

            Receive Open(State state) =>
                async (IContext context) => {
                    switch (context.Message) {
                        case Close close: {
                            var promise = new TaskCompletionSource();
                            context.Spawn(ChannelCloseActor.Create(state, close.Timeout, promise));

                            try {
                                await promise.Task;
                                close.SetResult();
                                context.Stop(context.Self!);
                            }
                            catch (Exception fault) {
                                close.SetException(fault);
                            }
                            break;
                        }
                        case DeclareExchange declare: {
                            var promise = new TaskCompletionSource();
                            context.Spawn(ExchangeDeclareActor.Create(state, declare.Definition, declare.Timeout, promise));

                            try {
                                await promise.Task;
                                declare.SetResult();
                            }
                            catch (Exception fault) {
                                declare.SetException(fault);
                            }
                            break;
                        }
                        case DeleteExchange delete: {
                            var promise = new TaskCompletionSource();
                            context.Spawn(ExchangeDeleteActor.Create(state, delete.Exchange, delete.Condition, delete.Timeout, promise));

                            try {
                                await promise.Task;
                                delete.SetResult();
                            }
                            catch (Exception fault) {
                                delete.SetException(fault);
                            }
                            break;
                        }
                        case DeclareQueue declare: {
                            var promise = new TaskCompletionSource();
                            context.Spawn(QueueDeclareActor.Create(state, declare.Definition, declare.Timeout, promise));

                            try {
                                await promise.Task;
                                declare.SetResult();
                            }
                            catch (Exception fault) {
                                declare.SetException(fault);
                            }
                            break;
                        }
                        case DeleteQueue delete: {
                            var promise = new TaskCompletionSource();
                            context.Spawn(QueueDeleteActor.Create(state, delete.Queue, delete.Condition, delete.Timeout, promise));

                            try {
                                await promise.Task;
                                delete.SetResult();
                            }
                            catch (Exception fault) {
                                delete.SetException(fault);
                            }
                            break;
                        }
                        case BindQueue bind: {
                            context.Send(state.Dispatcher, Dispatch.Command(new QueueBind(
                                queueName   : bind.Binding.Queue,
                                exchangeName: bind.Binding.Exchange,
                                routingKey  : bind.Binding.RoutingKey,
                                noWait      : false,
                                arguments   : bind.Binding.Arguments
                            )));
                            _behaviour.BecomeStacked(Awaiting<QueueBindOk>(state,
                                onReceive: _ => {
                                    bind.SetResult();
                                    _behaviour.UnbecomeStacked();
                                },
                                onChannelClosed: error => {
                                    bind.SetException(error);
                                }
                            ));
                            break;
                        }
                        case UnbindQueue unbind: {
                            context.Send(state.Dispatcher, Dispatch.Command(new QueueUnbind(
                                queueName   : unbind.Binding.Queue,
                                exchangeName: unbind.Binding.Exchange,
                                routingKey  : unbind.Binding.RoutingKey,
                                arguments   : unbind.Binding.Arguments
                            )));
                            _behaviour.BecomeStacked(Awaiting<QueueUnbindOk>(state,
                                onReceive: _ => {
                                    unbind.SetResult();
                                    _behaviour.UnbecomeStacked();
                                },
                                onChannelClosed: error => {
                                    unbind.SetException(error);
                                }
                            ));
                            break;
                        }
                        case PurgeQueue purge: {
                            context.Send(state.Dispatcher, Dispatch.Command(new QueuePurge(
                                queueName: purge.Queue,
                                noWait   : false
                            )));
                            _behaviour.BecomeStacked(Awaiting<QueuePurgeOk>(state,
                                onReceive: _ => {
                                    purge.SetResult();
                                    _behaviour.UnbecomeStacked();
                                },
                                onChannelClosed: error => {
                                    purge.SetException(error);
                                }
                            ));
                            break;
                        }
                        case Publish publish: {
                            context.Send(state.Dispatcher, Dispatch.Command(new BasicPublish(
                                exchangeName: publish.Exchange,
                                routingKey  : publish.RoutingKey,
                                mandatory   : publish.Mandatory,
                                immediate   : publish.Immediate
                            )));
                            context.Send(state.Dispatcher, Dispatch.ContentHeader(new ContentHeader(
                                ClassId   : 0x3C,
                                BodySize  : (UInt64)publish.Message.Body.Length,
                                Properties: publish.Message.Properties
                            )));

                            foreach (var segment in publish.Message.Body.Split((Int32)_maxFrameSize)) {
                                context.Send(state.Dispatcher, Dispatch.ContentBody(segment));
                            }

                            if (publish.Mandatory || publish.Immediate) {
                                _behaviour.BecomeStacked(Awaiting<BasicReturn>(state,
                                    onReceive: @return => {
                                        publish.SetException(AmqpException.Create(@return.ReplyCode, @return.ReplyText));
                                        _behaviour.UnbecomeStacked();
                                    },
                                    onUnexpected: context => {
                                        _behaviour.UnbecomeStacked();
                                        _behaviour.ReceiveAsync(context);
                                    },
                                    onChannelClosed: error => {
                                        publish.SetException(error);
                                    }
                                ));
                                break;
                            }
                            else {
                                publish.SetResult();
                            }
                            break;
                        }
                        case GetMessage get: {
                            context.Send(state.Dispatcher, Dispatch.Command(new BasicGet(
                                queueName: get.Queue,
                                noAck    : get.Acknowledgements switch {
                                    Acknowledgements.Auto   => true,
                                    Acknowledgements.Manual => false,
                                    _                       => false
                                }
                            )));
                            _behaviour.BecomeStacked(AwaitingGetOkOrEmpty(get));
                            break;
                        }
                        case Acknowledge ack: {
                            context.Send(state.Dispatcher, Dispatch.Command(new BasicAck(
                                deliveryTag: ack.DeliveryTag,
                                multiple   : ack.Multiple
                            )));
                            ack.SetResult();
                            break;
                        }
                        case Reject reject: {
                            context.Send(state.Dispatcher, Dispatch.Command(new BasicReject(
                                deliveryTag: reject.DeliveryTag,
                                requeue    : reject.Requeue
                            )));
                            reject.SetResult();
                            break;
                        }
                        case SetPrefetchLimit prefetch: {
                            context.Send(state.Dispatcher, Dispatch.Command(new BasicQos(
                                prefetchSize : 0,
                                prefetchCount: prefetch.Limit,
                                global       : prefetch.Global
                            )));
                            _behaviour.BecomeStacked(Awaiting<BasicQosOk>(state,
                                onReceive: _ => {
                                    prefetch.SetResult();
                                    _behaviour.UnbecomeStacked();
                                },
                                onChannelClosed: error => {
                                    prefetch.SetException(error);
                                }
                            ));
                            break;
                        }
                        case Consume consume: {
                            var consumerTag = $"{Guid.NewGuid()}";
                            context.Send(state.Dispatcher, Dispatch.Command(new BasicConsume(
                                queueName  : consume.Queue,
                                consumerTag: consumerTag,
                                noLocal    : false,
                                noAck      : consume.ConsumerConfiguration.Acknowledgements switch {
                                    Acknowledgements.Auto => true,
                                    _                     => false
                                },
                                exclusive  : consume.ConsumerConfiguration.Exclusive,
                                noWait     : false,
                                arguments  : consume.Arguments ?? ImmutableDictionary<String, Object>.Empty
                            )));
                            _behaviour.BecomeStacked(Awaiting<BasicConsumeOk>(state,
                                onReceive: _ => {
                                    var consumer = context.SpawnNamed(
                                        name: $"consumer_{consumerTag}",
                                        props: ConsumerAgent.Create()
                                    );
                                    context.Send(consumer, new StartConsuming(
                                        ConsumerTag          : consumerTag,
                                        Dispatcher           : state.Dispatcher,
                                        ConsumerConfiguration: consume.ConsumerConfiguration
                                    ));
                                    _behaviour.Become(Open(state with {
                                        Consumers = state.Consumers.Add(consumerTag, consumer)
                                    }));
                                    consume.SetResult(consumerTag);
                                },
                                onChannelClosed: error => {
                                    consume.SetException(error);
                                }
                            ));
                            break;
                        }
                        case BasicDeliver deliver: {
                            if (state.Consumers.ContainsKey(deliver.ConsumerTag)) {
                                var consumer = state.Consumers[deliver.ConsumerTag];
                                _behaviour.BecomeStacked(AwaitingContentHeader(
                                    delivery: DeliveryInfo.FromBasicDeliver(deliver),
                                    consumer: consumer
                                ));
                            }
                            else {
                                // TODO: No handler....
                            }
                            break;
                        }
                    }
                };

            Receive Awaiting<T>(State state, Action<T> onReceive, Action<IContext>? onUnexpected = null, Action<Exception>? onChannelClosed = null) =>
                (IContext context) => {
                    switch (context.Message) {
                        case T expected: {
                            onReceive(expected);
                            break;
                        }
                        case ChannelClose close: {
                            var exception = AmqpException.Create(close.ReplyCode, close.ReplyText);
                            onChannelClosed?.Invoke(exception);
                            context.Send(state.Dispatcher, Dispatch.Command(new ChannelCloseOk()));
                            context.System.EventStream.Unsubscribe(state.SubscriptionId);
                            _behaviour.Become(Closed);
                            break;
                        }
                        default: {
                            onUnexpected?.Invoke(context);
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingGetOkOrEmpty(GetMessage getMessage) =>
                (IContext context) => {
                    switch (context.Message) {
                        case BasicGetEmpty _: {
                            getMessage.SetResult(null);
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                        case BasicGetOk ok: {
                            _behaviour.UnbecomeStacked();
                            _behaviour.BecomeStacked(AwaitingContentHeader(DeliveryInfo.FromBasicGetOk(ok), getMessage));
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingContentHeader(DeliveryInfo delivery, GetMessage getMessage) {
                return (IContext context) => {
                    switch (context.Message) {
                        case ContentHeader header: {
                            _behaviour.UnbecomeStacked();
                            switch (header.BodySize) {
                                case 0: {
                                    getMessage.SetResult((delivery, header.Properties, Memory<Byte>.Empty));
                                    break;
                                }
                                default: {
                                    _behaviour.BecomeStacked(AwaitingContentBody(delivery, header, getMessage));
                                    break;
                                }
                            }
                            break;
                        }
                    }
                    return CompletedTask;
                };
            }

            Receive AwaitingContentBody(DeliveryInfo delivery, ContentHeader header, GetMessage getMessage) {
                var buffer = new ArrayBufferWriter<Byte>((Int32)header.BodySize);
                return (IContext context) => {
                    switch (context.Message) {
                        case ReadOnlyMemory<Byte> bodySegment: {
                            buffer.Write(bodySegment.Span);

                            if ((UInt64)buffer.WrittenCount >= header.BodySize) {
                                getMessage.SetResult((delivery, header.Properties, buffer.WrittenMemory));
                                _behaviour.UnbecomeStacked();
                            }

                            break;
                        }
                    }
                    return CompletedTask;
                };
            }

            Receive AwaitingContentHeader(DeliveryInfo delivery, PID consumer) =>
                (IContext context) => {
                    switch (context.Message) {
                        case ContentHeader header: {
                            _behaviour.UnbecomeStacked();
                            switch (header.BodySize) {
                                case 0: {
                                    context.Send(consumer, new ConsumeMessage(
                                        Delivery  : delivery,
                                        Properties: header.Properties,
                                        Buffer    : new MemoryBufferWriter<Byte>()
                                    ));
                                    break;
                                }
                                default: {
                                    _behaviour.BecomeStacked(AwaitingContentBody(
                                        delivery    : delivery,
                                        consumer    : consumer,
                                        properties  : header.Properties,
                                        expectedSize: header.BodySize
                                    ));
                                    break;
                                }
                            }
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingContentBody(DeliveryInfo delivery, PID consumer, BasicProperties properties, UInt64 expectedSize) {
                var buffer = new MemoryBufferWriter<Byte>((Int32)expectedSize);

                return (IContext context) => {
                    switch (context.Message) {
                        case ReadOnlyMemory<Byte> segment: {
                            buffer.Write(segment.Span);
                            if ((UInt64)buffer.WrittenCount >= expectedSize) {
                                context.Send(consumer, new ConsumeMessage(delivery, properties, buffer));
                                _behaviour.UnbecomeStacked();
                            }
                            break;
                        }
                    }

                    return CompletedTask;
                };
            }
        }

        class ChannelOpenActor : IActor {
            readonly Behavior _behaviour;
            readonly State _state;
            readonly TimeSpan _timeout;
            readonly TaskCompletionSource _promise;

            public ChannelOpenActor(State state, TimeSpan timeout, TaskCompletionSource promise) {
                _behaviour = new Behavior(Unstarted);
                _state     = state;
                _timeout   = timeout;
                _promise   = promise;
            }

            static public Props Create(State state, TimeSpan timeout, TaskCompletionSource promise) =>
                Props.FromProducer(() => new ChannelOpenActor(state, timeout, promise))
                    .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames());

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Unstarted(IContext context) {
                switch (context.Message) {
                    case Started: {
                        var subscription = context.System.EventStream.Subscribe<FrameReceived>(
                            predicate: message => message.Frame.Channel == _state.ChannelId,
                            action   : message => context.Send(context.Self!, message)
                        );
                        context.Send(_state.Dispatcher, Dispatch.Command(new ChannelOpen()));
                        var scheduledTimeout = context.Scheduler().SendOnce(_timeout, context.Self!, new TimeoutException());
                        _behaviour.Become(AwaitingChannelOpenOk(subscription, scheduledTimeout));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive AwaitingChannelOpenOk(EventStreamSubscription<Object> subscription, CancellationTokenSource scheduledTimeout) =>
                context => {
                    switch (context.Message) {
                        case ChannelOpenOk: {
                            scheduledTimeout!.Cancel();
                            _promise.SetResult();
                            context.Stop(context.Self!);
                            break;
                        }
                        case TimeoutException timeout: {
                            _promise.SetException(timeout);
                            context.Stop(context.Self!);
                            break;
                        }
                        case Stopping: {
                            subscription!.Unsubscribe();
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }

        class ChannelCloseActor : IActor {
            readonly Behavior _behaviour;
            readonly State _state;
            readonly TimeSpan _timeout;
            readonly TaskCompletionSource _promise;

            public ChannelCloseActor(State state, TimeSpan timeout, TaskCompletionSource promise) {
                _behaviour = new Behavior(Unstarted);
                _state     = state;
                _timeout   = timeout;
                _promise   = promise;
            }

            static public Props Create(State state, TimeSpan timeout, TaskCompletionSource promise) =>
                Props.FromProducer(() => new ChannelCloseActor(state, timeout, promise))
                    .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames());

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Unstarted(IContext context) {
                switch (context.Message) {
                    case Started: {
                        var subscription = context.System.EventStream.Subscribe<FrameReceived>(
                            predicate: message => message.Frame.Channel == _state.ChannelId,
                            action   : message => context.Send(context.Self!, message)
                        );
                        context.Send(_state.Dispatcher, Dispatch.Command(new ChannelClose(0, String.Empty, (0, 0))));
                        var scheduledTimeout = context.Scheduler().SendOnce(_timeout, context.Self!, new TimeoutException());
                        _behaviour.Become(AwaitingChannelCloseOk(subscription, scheduledTimeout));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive AwaitingChannelCloseOk(EventStreamSubscription<Object> subscription, CancellationTokenSource scheduledTimeout) =>
                context => {
                    switch (context.Message) {
                        case ChannelCloseOk: {
                            scheduledTimeout.Cancel();
                            _promise.SetResult();
                            context.Stop(context.Self!);
                            break;
                        }
                        case TimeoutException timeout: {
                            _promise.SetException(timeout);
                            context.Stop(context.Self!);
                            break;
                        }
                        case Stopping: {
                            subscription!.Unsubscribe();
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }

        class ExchangeDeclareActor : IActor {
            readonly Behavior _behaviour;
            readonly State _state;
            readonly ExchangeDefinition _definition;
            readonly TimeSpan _timeout;
            readonly TaskCompletionSource _promise;

            public ExchangeDeclareActor(State state, ExchangeDefinition definition, TimeSpan timeout, TaskCompletionSource promise) {
                _behaviour  = new Behavior(Unstarted);
                _state      = state;
                _timeout    = timeout;
                _definition = definition;
                _promise    = promise;
            }

            static public Props Create(State state, ExchangeDefinition definition, TimeSpan timeout, TaskCompletionSource promise) =>
                Props.FromProducer(() => new ExchangeDeclareActor(state, definition, timeout, promise))
                    .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames());

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Unstarted(IContext context) {
                switch (context.Message) {
                    case Started: {
                        var subscription = context.System.EventStream.Subscribe<FrameReceived>(
                            predicate: message => message.Frame.Channel == _state.ChannelId,
                            action   : message => context.Send(context.Self!, message)
                        );
                        context.Send(_state.Dispatcher, Dispatch.Command(new ExchangeDeclare(
                            exchangeName: _definition.Name,
                            exchangeType: _definition.Type,
                            passive     : false,
                            durable     : _definition.Durability > Durability.Transient,
                            autoDelete  : _definition.AutoDelete,
                            @internal   : _definition.Internal,
                            noWait      : false,
                            arguments   : _definition.Arguments
                        )));
                        var scheduledTimeout = context.Scheduler().SendOnce(_timeout, context.Self!, new TimeoutException());
                        _behaviour.Become(AwaitingExchangeDeclareOk(subscription, scheduledTimeout));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive AwaitingExchangeDeclareOk(EventStreamSubscription<Object> subscription, CancellationTokenSource scheduledTimeout) =>
                context => {
                    switch (context.Message) {
                        case ExchangeDeclareOk: {
                            scheduledTimeout.Cancel();
                            _promise.SetResult();
                            context.Stop(context.Self!);
                            break;
                        }
                        case TimeoutException timeout: {
                            _promise.SetException(timeout);
                            context.Stop(context.Self!);
                            break;
                        }
                        case ChannelClose close: {
                            scheduledTimeout.Cancel();
                            _promise.SetException(AmqpException.Create(close.ReplyCode, close.ReplyText));
                            context.Stop(context.Self!);
                            break;
                        }
                        case Stopping: {
                            subscription!.Unsubscribe();
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }

        class ExchangeDeleteActor : IActor {
            readonly Behavior _behaviour;
            readonly State _state;
            readonly String _exchange;
            readonly DeleteExchangeCondition _condition;
            readonly TimeSpan _timeout;
            readonly TaskCompletionSource _promise;

            public ExchangeDeleteActor(State state, String exchange, DeleteExchangeCondition condition, TimeSpan timeout, TaskCompletionSource promise) {
                _behaviour = new Behavior(Unstarted);
                _state     = state;
                _timeout   = timeout;
                _exchange  = exchange;
                _condition = condition;
                _promise   = promise;
            }

            static public Props Create(State state, String exchange, DeleteExchangeCondition condition, TimeSpan timeout, TaskCompletionSource promise) =>
                Props.FromProducer(() => new ExchangeDeleteActor(state, exchange, condition, timeout, promise))
                    .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames());

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Unstarted(IContext context) {
                switch (context.Message) {
                    case Started: {
                        var subscription = context.System.EventStream.Subscribe<FrameReceived>(
                            predicate: message => message.Frame.Channel == _state.ChannelId,
                            action   : message => context.Send(context.Self!, message)
                        );
                        context.Send(_state.Dispatcher, Dispatch.Command(new ExchangeDelete(
                            exchangeName: _exchange,
                            ifUnused    : _condition.HasFlag(DeleteExchangeCondition.Unused),
                            noWait      : false
                        )));
                        var scheduledTimeout = context.Scheduler().SendOnce(_timeout, context.Self!, new TimeoutException());
                        _behaviour.Become(AwaitingExchangeDeleteOk(subscription, scheduledTimeout));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive AwaitingExchangeDeleteOk(EventStreamSubscription<Object> subscription, CancellationTokenSource scheduledTimeout) =>
                context => {
                    switch (context.Message) {
                        case ExchangeDeleteOk: {
                            scheduledTimeout.Cancel();
                            _promise.SetResult();
                            context.Stop(context.Self!);
                            break;
                        }
                        case TimeoutException timeout: {
                            _promise.SetException(timeout);
                            context.Stop(context.Self!);
                            break;
                        }
                        case ChannelClose close: {
                            scheduledTimeout.Cancel();
                            _promise.SetException(AmqpException.Create(close.ReplyCode, close.ReplyText));
                            context.Stop(context.Self!);
                            break;
                        }
                        case Stopping: {
                            subscription!.Unsubscribe();
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }

        class QueueDeclareActor : IActor {
            readonly Behavior _behaviour;
            readonly State _state;
            readonly QueueDefinition _definition;
            readonly TimeSpan _timeout;
            readonly TaskCompletionSource _promise;

            public QueueDeclareActor(State state, QueueDefinition definition, TimeSpan timeout, TaskCompletionSource promise) {
                _behaviour  = new Behavior(Unstarted);
                _state      = state;
                _timeout    = timeout;
                _definition = definition;
                _promise    = promise;
            }

            static public Props Create(State state, QueueDefinition definition, TimeSpan timeout, TaskCompletionSource promise) =>
                Props.FromProducer(() => new QueueDeclareActor(state, definition, timeout, promise))
                    .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames());

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Unstarted(IContext context) {
                switch (context.Message) {
                    case Started: {
                        var subscription = context.System.EventStream.Subscribe<FrameReceived>(
                            predicate: message => message.Frame.Channel == _state.ChannelId,
                            action   : message => context.Send(context.Self!, message)
                        );
                        context.Send(_state.Dispatcher, Dispatch.Command(new QueueDeclare(
                            queueName : _definition.Name,
                            passive   : false,
                            durable   : _definition.Durability > Durability.Transient,
                            exclusive : _definition.Exclusive,
                            autoDelete: _definition.AutoDelete,
                            noWait    : false,
                            arguments : _definition.Arguments
                        )));
                        var scheduledTimeout = context.Scheduler().SendOnce(_timeout, context.Self!, new TimeoutException());
                        _behaviour.Become(AwaitingQueueDeclareOk(subscription, scheduledTimeout));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive AwaitingQueueDeclareOk(EventStreamSubscription<Object> subscription, CancellationTokenSource scheduledTimeout) =>
                context => {
                    switch (context.Message) {
                        case QueueDeclareOk ok: {
                            scheduledTimeout.Cancel();
                            _promise.SetResult();
                            context.Stop(context.Self!);
                            break;
                        }
                        case TimeoutException timeout: {
                            _promise.SetException(timeout);
                            context.Stop(context.Self!);
                            break;
                        }
                        case ChannelClose close: {
                            scheduledTimeout.Cancel();
                            _promise.SetException(AmqpException.Create(close.ReplyCode, close.ReplyText));
                            context.Stop(context.Self!);
                            break;
                        }
                        case Stopping: {
                            subscription!.Unsubscribe();
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }

        class QueueDeleteActor : IActor {
            readonly Behavior _behaviour;
            readonly State _state;
            readonly String _queue;
            readonly DeleteQueueCondition _condition;
            readonly TimeSpan _timeout;
            readonly TaskCompletionSource _promise;

            public QueueDeleteActor(State state, String queue, DeleteQueueCondition condition, TimeSpan timeout, TaskCompletionSource promise) {
                _behaviour = new Behavior(Unstarted);
                _state     = state;
                _timeout   = timeout;
                _queue     = queue;
                _condition = condition;
                _promise   = promise;
            }

            static public Props Create(State state, String queue, DeleteQueueCondition condition, TimeSpan timeout, TaskCompletionSource promise) =>
                Props.FromProducer(() => new QueueDeleteActor(state, queue, condition, timeout, promise))
                    .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames());

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Unstarted(IContext context) {
                switch (context.Message) {
                    case Started: {
                        var subscription = context.System.EventStream.Subscribe<FrameReceived>(
                            predicate: message => message.Frame.Channel == _state.ChannelId,
                            action   : message => context.Send(context.Self!, message)
                        );
                        context.Send(_state.Dispatcher, Dispatch.Command(new QueueDelete(
                            queueName: _queue,
                            ifUnused : _condition.HasFlag(DeleteQueueCondition.Unused),
                            ifEmpty  : _condition.HasFlag(DeleteQueueCondition.Empty),
                            noWait   : false
                        )));
                        var scheduledTimeout = context.Scheduler().SendOnce(_timeout, context.Self!, new TimeoutException());
                        _behaviour.Become(AwaitingQueueDeleteOk(subscription, scheduledTimeout));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive AwaitingQueueDeleteOk(EventStreamSubscription<Object> subscription, CancellationTokenSource scheduledTimeout) =>
                context => {
                    switch (context.Message) {
                        case QueueDeleteOk: {
                            scheduledTimeout.Cancel();
                            _promise.SetResult();
                            context.Stop(context.Self!);
                            break;
                        }
                        case TimeoutException timeout: {
                            _promise.SetException(timeout);
                            context.Stop(context.Self!);
                            break;
                        }
                        case ChannelClose close: {
                            scheduledTimeout.Cancel();
                            _promise.SetException(AmqpException.Create(close.ReplyCode, close.ReplyText));
                            context.Stop(context.Self!);
                            break;
                        }
                        case Stopping: {
                            subscription!.Unsubscribe();
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }
    }
}
