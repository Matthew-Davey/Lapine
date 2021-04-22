namespace Lapine.Agents {
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lapine.Agents.Middleware;
    using Lapine.Client;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.DispatcherAgent.Protocol;
    using static Lapine.Agents.ChannelAgent.Protocol;

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
        }

        static public Props Create(UInt32 maxFrameSize) =>
            Props.FromProducer(() => new Actor(maxFrameSize))
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames())
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundContentHeaderFrames())
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundContentBodyFrames());

        record State(PID Dispatcher);

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
                            )
                        );
                        context.Send(state.Dispatcher, new DispatchTo(open.TxD, open.ChannelId));
                        context.Send(state.Dispatcher, new ChannelOpen());
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
                            context.Send(state.Dispatcher, new ChannelClose(0, String.Empty, (0, 0)));
                            _behaviour.Become(AwaitingChannelCloseOk(close.Promise));
                            break;
                        }
                        case DeclareExchange declare: {
                            context.Send(state.Dispatcher, new ExchangeDeclare(
                                exchangeName: declare.Definition.Name,
                                exchangeType: declare.Definition.Type,
                                passive     : false,
                                durable     : declare.Definition.Durability > Durability.Ephemeral,
                                autoDelete  : declare.Definition.AutoDelete,
                                @internal   : false,
                                noWait      : false,
                                arguments   : declare.Definition.Arguments
                            ));
                            _behaviour.BecomeStacked(AwaitingExchangeDeclareOk(state, declare.Promise));
                            break;
                        }
                        case DeleteExchange delete: {
                            context.Send(state.Dispatcher, new ExchangeDelete(
                                exchangeName: delete.Exchange,
                                ifUnused    : delete.Condition.HasFlag(DeleteExchangeCondition.Unused),
                                noWait      : false
                            ));
                            _behaviour.BecomeStacked(AwaitingExchangeDeleteOk(delete.Promise));
                            break;
                        }
                        case DeclareQueue declare: {
                            context.Send(state.Dispatcher, new QueueDeclare(
                                queueName : declare.Definition.Name,
                                passive   : false,
                                durable   : declare.Definition.Durability > Durability.Ephemeral,
                                exclusive : declare.Definition.Exclusive,
                                autoDelete: declare.Definition.AutoDelete,
                                noWait    : false,
                                arguments : declare.Definition.Arguments
                            ));
                            _behaviour.BecomeStacked(AwaitingQueueDeclareOk(declare.Promise));
                            break;
                        }
                        case DeleteQueue delete: {
                            context.Send(state.Dispatcher, new QueueDelete(
                                queueName: delete.Queue,
                                ifUnused : delete.Condition.HasFlag(DeleteQueueCondition.Unused),
                                ifEmpty  : delete.Condition.HasFlag(DeleteQueueCondition.Empty),
                                noWait   : false
                            ));
                            _behaviour.BecomeStacked(AwaitingQueueDeleteOk(delete.Promise));
                            break;
                        }
                        case BindQueue bind: {
                            context.Send(state.Dispatcher, new QueueBind(
                                queueName   : bind.Queue,
                                exchangeName: bind.Exchange,
                                routingKey  : bind.RoutingKey,
                                noWait      : false,
                                arguments   : bind.Arguments
                            ));
                            _behaviour.BecomeStacked(AwaitingQueueBindOk(bind.Promise));
                            break;
                        }
                        case UnbindQueue unbind: {
                            context.Send(state.Dispatcher, new QueueUnbind(
                                queueName   : unbind.Queue,
                                exchangeName: unbind.Exchange,
                                routingKey  : unbind.RoutingKey,
                                arguments   : unbind.Arguments
                            ));
                            _behaviour.BecomeStacked(AwaitingQueueUnbindOk(unbind.Promise));
                            break;
                        }
                        case PurgeQueue purge: {
                            context.Send(state.Dispatcher, new QueuePurge(
                                queueName: purge.Queue,
                                noWait   : false
                            ));
                            _behaviour.BecomeStacked(AwaitingQueuePurgeOk(purge.Promise));
                            break;
                        }
                        case Publish publish: {
                            context.Send(state.Dispatcher, new BasicPublish(publish.Exchange, publish.RoutingKey, publish.Mandatory, publish.Immediate));
                            context.Send(state.Dispatcher, new ContentHeader(0x3C, (UInt64)publish.Message.Body.Length, publish.Message.Properties));

                            foreach (var segment in publish.Message.Body.Split((Int32)_maxFrameSize)) {
                                context.Send(state.Dispatcher, segment);
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
                            context.Send(state.Dispatcher, new BasicGet(get.Queue, !get.Ack));
                            _behaviour.BecomeStacked(AwaitingGetOkOrEmpty(get.Promise));
                            break;
                        }
                        case SetPrefetchLimit prefetch: {
                            context.Send(state.Dispatcher, new BasicQos(0, prefetch.Limit, prefetch.Global));
                            _behaviour.BecomeStacked(AwaitingBasicQosOk(prefetch.Promise));
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
                            context.Send(state.Dispatcher, new ChannelCloseOk());
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

            Receive AwaitingQueueDeclareOk(TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case QueueDeclareOk _: {
                            promise.SetResult();
                            _behaviour.UnbecomeStacked();
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
                            if (header.BodySize == 0) {
                                promise.SetResult((delivery, header.Properties, Array.Empty<Byte>()));
                                _behaviour.UnbecomeStacked();
                            }
                            else {
                                _behaviour.UnbecomeStacked();
                                _behaviour.BecomeStacked(AwaitingContentBody(delivery, header, promise));
                                break;
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
        }
    }
}
