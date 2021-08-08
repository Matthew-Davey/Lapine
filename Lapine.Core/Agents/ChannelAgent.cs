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
            public record Open(UInt16 ChannelId, PID TxD, TimeSpan Timeout, TaskCompletionSource Promise);
            public record Close(TaskCompletionSource Promise);
            public record DeclareExchange(ExchangeDefinition Definition, TaskCompletionSource Promise);
            public record DeleteExchange(
                String Exchange,
                DeleteExchangeCondition Condition,
                TaskCompletionSource Promise
            );
            public record DeclareQueue(QueueDefinition Definition, TaskCompletionSource Promise);
            public record DeleteQueue(String Queue, DeleteQueueCondition Condition, TaskCompletionSource Promise);
            public record BindQueue(
                String Exchange,
                String Queue,
                String RoutingKey,
                IReadOnlyDictionary<String, Object> Arguments,
                TaskCompletionSource Promise
            );
            public record UnbindQueue(
                String Exchange,
                String Queue,
                String RoutingKey,
                IReadOnlyDictionary<String, Object> Arguments,
                TaskCompletionSource Promise
            );
            public record PurgeQueue(String Queue, TaskCompletionSource Promise);
            public record Publish(
                String Exchange,
                String RoutingKey,
                (BasicProperties Properties, ReadOnlyMemory<Byte> Body) Message,
                Boolean Mandatory,
                Boolean Immediate,
                TaskCompletionSource Promise
            );
            public record GetMessage(
                String Queue,
                Acknowledgements Acknowledgements,
                TaskCompletionSource<(DeliveryInfo, BasicProperties, ReadOnlyMemory<Byte>)?> Promise
            );
            public record Acknowledge(UInt64 DeliveryTag, Boolean Multiple, TaskCompletionSource Promise);
            public record Reject(UInt64 DeliveryTag, Boolean Requeue, TaskCompletionSource Promise);
            public record SetPrefetchLimit(UInt16 Limit, Boolean Global, TaskCompletionSource Promise);
            public record Consume(
                String Queue,
                ConsumerConfiguration ConsumerConfiguration,
                IReadOnlyDictionary<String, Object>? Arguments,
                TaskCompletionSource<String> Promise
            );
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
                            open.Promise.SetResult();
                            _behaviour.Become(Open(state));
                        }
                        catch (Exception fault) {
                            open.Promise.SetException(fault);
                        }
                        break;
                    }
                }
            }

            Receive Open(State state) =>
                (IContext context) => {
                    switch (context.Message) {
                        case Close close: {
                            context.Send(state.Dispatcher, Dispatch.Command(new ChannelClose(0, String.Empty, (0, 0))));
                            _behaviour.Become(Awaiting<ChannelCloseOk>(state,
                                onReceive: _ => {
                                    close.Promise.SetResult();
                                    context.System.EventStream.Unsubscribe(state.SubscriptionId);
                                    context.Stop(context.Self!);
                                }
                            ));
                            break;
                        }
                        case DeclareExchange declare: {
                            context.Send(state.Dispatcher, Dispatch.Command(new ExchangeDeclare(
                                exchangeName: declare.Definition.Name,
                                exchangeType: declare.Definition.Type,
                                passive     : false,
                                durable     : declare.Definition.Durability > Durability.Transient,
                                autoDelete  : declare.Definition.AutoDelete,
                                @internal   : declare.Definition.Internal,
                                noWait      : false,
                                arguments   : declare.Definition.Arguments
                            )));
                            _behaviour.BecomeStacked(Awaiting<ExchangeDeclareOk>(state,
                                onReceive: _ => {
                                    declare.Promise.SetResult();
                                    _behaviour.UnbecomeStacked();
                                },
                                onChannelClosed: error => {
                                    declare.Promise.SetException(error);
                                }
                            ));
                            break;
                        }
                        case DeleteExchange delete: {
                            context.Send(state.Dispatcher, Dispatch.Command(new ExchangeDelete(
                                exchangeName: delete.Exchange,
                                ifUnused    : delete.Condition.HasFlag(DeleteExchangeCondition.Unused),
                                noWait      : false
                            )));
                            _behaviour.BecomeStacked(Awaiting<ExchangeDeleteOk>(state,
                                onReceive: _ => {
                                    delete.Promise.SetResult();
                                    _behaviour.UnbecomeStacked();
                                },
                                onChannelClosed: error => {
                                    delete.Promise.SetException(error);
                                }
                            ));
                            break;
                        }
                        case DeclareQueue declare: {
                            context.Send(state.Dispatcher, Dispatch.Command(new QueueDeclare(
                                queueName : declare.Definition.Name,
                                passive   : false,
                                durable   : declare.Definition.Durability > Durability.Transient,
                                exclusive : declare.Definition.Exclusive,
                                autoDelete: declare.Definition.AutoDelete,
                                noWait    : false,
                                arguments : declare.Definition.Arguments
                            )));
                            _behaviour.BecomeStacked(Awaiting<QueueDeclareOk>(state,
                                onReceive: _ => {
                                    declare.Promise.SetResult();
                                    _behaviour.UnbecomeStacked();
                                },
                                onChannelClosed: error => {
                                    declare.Promise.SetException(error);
                                }
                            ));
                            break;
                        }
                        case DeleteQueue delete: {
                            context.Send(state.Dispatcher, Dispatch.Command(new QueueDelete(
                                queueName: delete.Queue,
                                ifUnused : delete.Condition.HasFlag(DeleteQueueCondition.Unused),
                                ifEmpty  : delete.Condition.HasFlag(DeleteQueueCondition.Empty),
                                noWait   : false
                            )));
                            _behaviour.BecomeStacked(Awaiting<QueueDeleteOk>(state,
                                onReceive: _ => {
                                    delete.Promise.SetResult();
                                    _behaviour.UnbecomeStacked();
                                },
                                onChannelClosed: error => {
                                    delete.Promise.SetException(error);
                                }
                            ));
                            break;
                        }
                        case BindQueue bind: {
                            context.Send(state.Dispatcher, Dispatch.Command(new QueueBind(
                                queueName   : bind.Queue,
                                exchangeName: bind.Exchange,
                                routingKey  : bind.RoutingKey,
                                noWait      : false,
                                arguments   : bind.Arguments
                            )));
                            _behaviour.BecomeStacked(Awaiting<QueueBindOk>(state,
                                onReceive: _ => {
                                    bind.Promise.SetResult();
                                    _behaviour.UnbecomeStacked();
                                },
                                onChannelClosed: error => {
                                    bind.Promise.SetException(error);
                                }
                            ));
                            break;
                        }
                        case UnbindQueue unbind: {
                            context.Send(state.Dispatcher, Dispatch.Command(new QueueUnbind(
                                queueName   : unbind.Queue,
                                exchangeName: unbind.Exchange,
                                routingKey  : unbind.RoutingKey,
                                arguments   : unbind.Arguments
                            )));
                            _behaviour.BecomeStacked(Awaiting<QueueUnbindOk>(state,
                                onReceive: _ => {
                                    unbind.Promise.SetResult();
                                    _behaviour.UnbecomeStacked();
                                },
                                onChannelClosed: error => {
                                    unbind.Promise.SetException(error);
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
                                    purge.Promise.SetResult();
                                    _behaviour.UnbecomeStacked();
                                },
                                onChannelClosed: error => {
                                    purge.Promise.SetException(error);
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
                                        publish.Promise.SetException(AmqpException.Create(@return.ReplyCode, @return.ReplyText));
                                        _behaviour.UnbecomeStacked();
                                    },
                                    onUnexpected: context => {
                                        _behaviour.UnbecomeStacked();
                                        _behaviour.ReceiveAsync(context);
                                    },
                                    onChannelClosed: error => {
                                        publish.Promise.SetException(error);
                                    }
                                ));
                                break;
                            }
                            else {
                                publish.Promise.SetResult();
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
                            _behaviour.BecomeStacked(AwaitingGetOkOrEmpty(get.Promise));
                            break;
                        }
                        case Acknowledge ack: {
                            context.Send(state.Dispatcher, Dispatch.Command(new BasicAck(
                                deliveryTag: ack.DeliveryTag,
                                multiple   : ack.Multiple
                            )));
                            ack.Promise.SetResult();
                            break;
                        }
                        case Reject reject: {
                            context.Send(state.Dispatcher, Dispatch.Command(new BasicReject(
                                deliveryTag: reject.DeliveryTag,
                                requeue    : reject.Requeue
                            )));
                            reject.Promise.SetResult();
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
                                    prefetch.Promise.SetResult();
                                    _behaviour.UnbecomeStacked();
                                },
                                onChannelClosed: error => {
                                    prefetch.Promise.SetException(error);
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
                                    consume.Promise.SetResult(consumerTag);
                                },
                                onChannelClosed: error => {
                                    consume.Promise.SetException(error);
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
                    return CompletedTask;
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

            Receive AwaitingGetOkOrEmpty(TaskCompletionSource<(DeliveryInfo, BasicProperties, ReadOnlyMemory<Byte>)?> promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case BasicGetEmpty _: {
                            promise.SetResult(null);
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                        case BasicGetOk ok: {
                            _behaviour.UnbecomeStacked();
                            _behaviour.BecomeStacked(AwaitingContentHeader(DeliveryInfo.FromBasicGetOk(ok), promise));
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingContentHeader(DeliveryInfo delivery, TaskCompletionSource<(DeliveryInfo, BasicProperties, ReadOnlyMemory<Byte>)?> promise) {
                return (IContext context) => {
                    switch (context.Message) {
                        case ContentHeader header: {
                            _behaviour.UnbecomeStacked();
                            switch (header.BodySize) {
                                case 0: {
                                    promise.SetResult((delivery, header.Properties, Memory<Byte>.Empty));
                                    break;
                                }
                                default: {
                                    _behaviour.BecomeStacked(AwaitingContentBody(delivery, header, promise));
                                    break;
                                }
                            }
                            break;
                        }
                    }
                    return CompletedTask;
                };
            }

            Receive AwaitingContentBody(DeliveryInfo delivery, ContentHeader header, TaskCompletionSource<(DeliveryInfo, BasicProperties, ReadOnlyMemory<Byte>)?> promise) {
                var buffer = new ArrayBufferWriter<Byte>((Int32)header.BodySize);
                return (IContext context) => {
                    switch (context.Message) {
                        case ReadOnlyMemory<Byte> bodySegment: {
                            buffer.Write(bodySegment.Span);

                            if ((UInt64)buffer.WrittenCount >= header.BodySize) {
                                promise.SetResult((delivery, header.Properties, buffer.WrittenMemory));
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
    }
}
