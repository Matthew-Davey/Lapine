namespace Lapine.Agents {
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using Lapine.Agents.Middleware;
    using Lapine.Client;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.DispatcherAgent.Protocol;
    using static Lapine.Agents.ChannelAgent.Protocol;
    using static Lapine.Agents.ConsumerAgent.Protocol;

    static class ChannelAgent {
        static public class Protocol {
            public record Open(PID Listener, UInt16 ChannelId, PID TxD);
            public record Opened(PID ChannelAgent);
            public record Close(TaskCompletionSource Promise);
            public record DeclareExchange(ExchangeDefinition Definition, TaskCompletionSource Promise);
            public record DeleteExchange(String Exchange, DeleteExchangeCondition Condition, TaskCompletionSource Promise);
            public record DeclareQueue(QueueDefinition Definition, TaskCompletionSource Promise);
            public record DeleteQueue(String Queue, DeleteQueueCondition Condition, TaskCompletionSource Promise);
            public record BindQueue(String Exchange, String Queue, String RoutingKey, IReadOnlyDictionary<String, Object> Arguments, TaskCompletionSource Promise);
            public record UnbindQueue(String Exchange, String Queue, String RoutingKey, IReadOnlyDictionary<String, Object> Arguments, TaskCompletionSource Promise);
            public record PurgeQueue(String Queue, TaskCompletionSource Promise);
            public record Publish(String Exchange, String RoutingKey, (BasicProperties Properties, ReadOnlyMemory<Byte> Body) Message, Boolean Mandatory, Boolean Immediate, TaskCompletionSource Promise);
            public record GetMessage(String Queue, Boolean Ack, TaskCompletionSource<(DeliveryInfo, BasicProperties, ReadOnlyMemory<Byte>)?> Promise);
            public record SetPrefetchLimit(UInt16 Limit, Boolean Global, TaskCompletionSource Promise);
            public record Consume(String Queue, ConsumerConfiguration ConsumerConfiguration, IReadOnlyDictionary<String, Object>? Arguments, TaskCompletionSource<String> Promise);
        }

        static public Props Create(UInt32 maxFrameSize) =>
            Props.FromProducer(() => new Actor(maxFrameSize))
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames())
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundContentHeaderFrames())
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundContentBodyFrames());

        record State(PID Dispatcher, IImmutableDictionary<String, PID> Consumers);

        class Actor : IActor {
            readonly UInt32 _maxFrameSize;
            readonly Behavior _behaviour;

            public Actor(UInt32 maxFrameSize) {
                _maxFrameSize = maxFrameSize;
                _behaviour    = new Behavior(Closed);
            }

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Closed(IContext context) {
                switch (context.Message) {
                    case Open open: {
                        var state = new State(
                            Dispatcher: context.SpawnNamed(
                                name: "dispatcher",
                                props: DispatcherAgent.Create()
                            ),
                            Consumers: ImmutableDictionary<String, PID>.Empty
                        );
                        context.Send(state.Dispatcher, new DispatchTo(open.TxD, open.ChannelId));
                        context.Send(state.Dispatcher, Dispatch.Command(new ChannelOpen()));
                        _behaviour.Become(Opening(state, open.Listener));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive Opening(State state, PID listener) =>
                (IContext context) => {
                    switch (context.Message) {
                        case ChannelOpenOk _: {
                            context.Send(listener, new Opened(context.Self!));
                            _behaviour.Become(Open(state));
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive Open(State state) =>
                (IContext context) => {
                    switch (context.Message) {
                        case Close close: {
                            context.Send(state.Dispatcher, Dispatch.Command(new ChannelClose(0, String.Empty, (0, 0))));
                            _behaviour.Become(AwaitingChannelCloseOk(close.Promise));
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
                            _behaviour.BecomeStacked(AwaitingExchangeDeclareOk(state, declare.Promise));
                            break;
                        }
                        case DeleteExchange delete: {
                            context.Send(state.Dispatcher, Dispatch.Command(new ExchangeDelete(
                                exchangeName: delete.Exchange,
                                ifUnused    : delete.Condition.HasFlag(DeleteExchangeCondition.Unused),
                                noWait      : false
                            )));
                            _behaviour.BecomeStacked(AwaitingExchangeDeleteOk(delete.Promise));
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
                            _behaviour.BecomeStacked(AwaitingQueueDeclareOk(state, declare.Promise));
                            break;
                        }
                        case DeleteQueue delete: {
                            context.Send(state.Dispatcher, Dispatch.Command(new QueueDelete(
                                queueName: delete.Queue,
                                ifUnused : delete.Condition.HasFlag(DeleteQueueCondition.Unused),
                                ifEmpty  : delete.Condition.HasFlag(DeleteQueueCondition.Empty),
                                noWait   : false
                            )));
                            _behaviour.BecomeStacked(AwaitingQueueDeleteOk(delete.Promise));
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
                            _behaviour.BecomeStacked(AwaitingQueueBindOk(bind.Promise));
                            break;
                        }
                        case UnbindQueue unbind: {
                            context.Send(state.Dispatcher, Dispatch.Command(new QueueUnbind(
                                queueName   : unbind.Queue,
                                exchangeName: unbind.Exchange,
                                routingKey  : unbind.RoutingKey,
                                arguments   : unbind.Arguments
                            )));
                            _behaviour.BecomeStacked(AwaitingQueueUnbindOk(unbind.Promise));
                            break;
                        }
                        case PurgeQueue purge: {
                            context.Send(state.Dispatcher, Dispatch.Command(new QueuePurge(
                                queueName: purge.Queue,
                                noWait   : false
                            )));
                            _behaviour.BecomeStacked(AwaitingQueuePurgeOk(purge.Promise));
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
                                classId   : 0x3C,
                                bodySize  : (UInt64)publish.Message.Body.Length,
                                properties: publish.Message.Properties
                            )));

                            foreach (var segment in publish.Message.Body.Split((Int32)_maxFrameSize)) {
                                context.Send(state.Dispatcher, Dispatch.ContentBody(segment));
                            }

                            if (publish.Mandatory || publish.Immediate) {
                                _behaviour.Become(AwaitingBasicReturn(publish.Promise));
                                break;
                            }
                            else {
                                publish.Promise.SetResult();
                            }
                            break;
                        }
                        case GetMessage get: {
                            context.Send(state.Dispatcher, Dispatch.Command(new BasicGet(get.Queue, !get.Ack)));
                            _behaviour.BecomeStacked(AwaitingGetOkOrEmpty(get.Promise));
                            break;
                        }
                        case SetPrefetchLimit prefetch: {
                            context.Send(state.Dispatcher, Dispatch.Command(new BasicQos(
                                prefetchSize : 0,
                                prefetchCount: prefetch.Limit,
                                global       : prefetch.Global
                            )));
                            _behaviour.BecomeStacked(AwaitingBasicQosOk(prefetch.Promise));
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
                            _behaviour.BecomeStacked(AwaitingConsumeOk(state, consumerTag, consume.ConsumerConfiguration, consume.Promise));
                            break;
                        }
                        case BasicDeliver deliver: {
                            var deliveryInfo = new DeliveryInfo(deliver.DeliveryTag, deliver.Redelivered, deliver.ExchangeName, null, null);

                            if (state.Consumers.ContainsKey(deliver.ConsumerTag)) {
                                var consumer = state.Consumers[deliver.ConsumerTag];
                                _behaviour.BecomeStacked(AwaitingContentHeader(deliveryInfo, consumer));
                            }
                            else {
                                // TODO: No handler....
                            }
                            break;
                        }
                    }
                    return CompletedTask;
                };

            static Receive AwaitingChannelCloseOk(TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case ChannelCloseOk _: {
                            promise.SetResult();
                            context.Stop(context.Self!);
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingExchangeDeclareOk(State state, TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case ExchangeDeclareOk _: {
                            promise.SetResult();
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                        case ChannelClose close: {
                            context.Send(state.Dispatcher, Dispatch.Command(new ChannelCloseOk()));
                            var exception = AmqpException.Create(close.ReplyCode, close.ReplyText);
                            promise.SetException(exception);
                            _behaviour.Become(Closed);

                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingExchangeDeleteOk(TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case ExchangeDeleteOk _: {
                            promise.SetResult();
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingQueueDeclareOk(State state, TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case QueueDeclareOk _: {
                            promise.SetResult();
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                        case ChannelClose close: {
                            context.Send(state.Dispatcher, Dispatch.Command(new ChannelCloseOk()));
                            var exception = AmqpException.Create(close.ReplyCode, close.ReplyText);
                            promise.SetException(exception);
                            _behaviour.Become(Closed);

                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingQueueDeleteOk(TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case QueueDeleteOk _: {
                            promise.SetResult();
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingQueueBindOk(TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case QueueBindOk _: {
                            promise.SetResult();
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingQueueUnbindOk(TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case QueueUnbindOk _: {
                            promise.SetResult();
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingQueuePurgeOk(TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case QueuePurgeOk _: {
                            promise.SetResult();
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingBasicReturn(TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case BasicReturn @return: {
                            promise.SetException(AmqpException.Create(@return.ReplyCode, @return.ReplyText));
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                        case ICommand _: {
                            promise.SetResult();
                            _behaviour.UnbecomeStacked();
                            return _behaviour.ReceiveAsync(context);
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
                            var deliveryInfo = new DeliveryInfo(ok.DeliveryTag, ok.Redelivered, ok.ExchangeName, ok.RoutingKey, ok.MessageCount);

                            _behaviour.UnbecomeStacked();
                            _behaviour.BecomeStacked(AwaitingContentHeader(deliveryInfo, promise));
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

            Receive AwaitingBasicQosOk(TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case BasicQosOk ok: {
                            promise.SetResult();
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingConsumeOk(State state, String consumerTag, ConsumerConfiguration consumerConfiguration, TaskCompletionSource<String> promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case BasicConsumeOk ok: {
                            var consumer = context.SpawnNamed(
                                name: $"consumer_{consumerTag}",
                                props: ConsumerAgent.Create()
                            );
                            context.Send(consumer, new StartConsuming(consumerTag, state.Dispatcher, consumerConfiguration));
                            _behaviour.Become(Open(state with {
                                Consumers = state.Consumers.Add(consumerTag, consumer)
                            }));
                            promise.SetResult(consumerTag);
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingContentHeader(DeliveryInfo delivery, PID consumer) =>
                (IContext context) => {
                    switch (context.Message) {
                        case ContentHeader header: {
                            _behaviour.UnbecomeStacked();
                            switch (header.BodySize) {
                                case 0: {
                                    context.Send(consumer, new ConsumeMessage(delivery, header.Properties, new MemoryBufferWriter<Byte>()));
                                    break;
                                }
                                default: {
                                    _behaviour.BecomeStacked(AwaitingContentBody(delivery, consumer, header.Properties, header.BodySize));
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
    }
}
