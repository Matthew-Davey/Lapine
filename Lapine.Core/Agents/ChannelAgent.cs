namespace Lapine.Agents {
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using Lapine.Agents.Middleware;
    using Lapine.Agents.ProcessManagers;
    using Lapine.Client;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;

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
            public record PurgeQueue(String Queue, TimeSpan Timeout) : AsyncCommand;
            public record Publish(
                String Exchange,
                String RoutingKey,
                RoutingFlags RoutingFlags,
                (BasicProperties Properties, ReadOnlyMemory<Byte> Body) Message,
                TimeSpan Timeout
            ) : AsyncCommand;
            public record GetMessage(
                String Queue,
                Acknowledgements Acknowledgements
            ) : AsyncCommand<(DeliveryInfo, BasicProperties, ReadOnlyMemory<Byte>)?>;
            public record Acknowledge(UInt64 DeliveryTag, Boolean Multiple) : AsyncCommand;
            public record Reject(UInt64 DeliveryTag, Boolean Requeue) : AsyncCommand;
            public record SetPrefetchLimit(UInt16 Limit, Boolean Global, TimeSpan Timeout) : AsyncCommand;
            public record Consume(
                String Queue,
                ConsumerConfiguration ConsumerConfiguration,
                IReadOnlyDictionary<String, Object>? Arguments,
                TimeSpan Timeout
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
                        context.Spawn(
                            RequestReplyProcessManager<ChannelOpen, ChannelOpenOk>.Create(
                                channelId : state.ChannelId,
                                dispatcher: state.Dispatcher,
                                request   : new ChannelOpen(),
                                timeout   : open.Timeout,
                                promise   : promise
                            )
                        );

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
                            context.Spawn(
                                RequestReplyProcessManager<ChannelClose, ChannelCloseOk>.Create(
                                    channelId : state.ChannelId,
                                    dispatcher: state.Dispatcher,
                                    request   : new ChannelClose(0, String.Empty, (0, 0)),
                                    timeout   : close.Timeout,
                                    promise   : promise
                                )
                            );

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
                            context.Spawn(
                                RequestReplyProcessManager<ExchangeDeclare, ExchangeDeclareOk>.Create(
                                    channelId : state.ChannelId,
                                    dispatcher: state.Dispatcher,
                                    request: new ExchangeDeclare(
                                        exchangeName: declare.Definition.Name,
                                        exchangeType: declare.Definition.Type,
                                        passive     : false,
                                        durable     : declare.Definition.Durability == Durability.Durable,
                                        autoDelete  : declare.Definition.AutoDelete,
                                        @internal   : false,
                                        noWait      : false,
                                        arguments   : declare.Definition.Arguments
                                    ),
                                    timeout   : declare.Timeout,
                                    promise   : promise
                                )
                            );

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
                            context.Spawn(
                                RequestReplyProcessManager<ExchangeDelete, ExchangeDeleteOk>.Create(
                                    channelId : state.ChannelId,
                                    dispatcher: state.Dispatcher,
                                    request: new ExchangeDelete(
                                        exchangeName: delete.Exchange,
                                        ifUnused    : delete.Condition.HasFlag(DeleteExchangeCondition.Unused),
                                        noWait      : false
                                    ),
                                    timeout   : delete.Timeout,
                                    promise   : promise
                                )
                            );

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
                            context.Spawn(
                                RequestReplyProcessManager<QueueDeclare, QueueDeclareOk>.Create(
                                    channelId : state.ChannelId,
                                    dispatcher: state.Dispatcher,
                                    request   : new QueueDeclare(
                                        queueName : declare.Definition.Name,
                                        passive   : false,
                                        durable   : declare.Definition.Durability == Durability.Durable,
                                        exclusive : declare.Definition.Exclusive,
                                        autoDelete: declare.Definition.AutoDelete,
                                        noWait    : false,
                                        arguments : declare.Definition.Arguments
                                    ),
                                    timeout   : declare.Timeout,
                                    promise   : promise
                                )
                            );

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
                            context.Spawn(
                                RequestReplyProcessManager<QueueDelete, QueueDeleteOk>.Create(
                                    channelId : state.ChannelId,
                                    dispatcher: state.Dispatcher,
                                    request   : new QueueDelete(
                                        queueName: delete.Queue,
                                        ifUnused : delete.Condition.HasFlag(DeleteQueueCondition.Unused),
                                        ifEmpty  : delete.Condition.HasFlag(DeleteQueueCondition.Empty),
                                        noWait   : false
                                    ),
                                    timeout   : delete.Timeout,
                                    promise   : promise
                                )
                            );

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
                            var promise = new TaskCompletionSource();
                            context.Spawn(
                                RequestReplyProcessManager<QueueBind, QueueBindOk>.Create(
                                    channelId : state.ChannelId,
                                    dispatcher: state.Dispatcher,
                                    request   : new QueueBind(
                                        queueName   : bind.Binding.Queue,
                                        exchangeName: bind.Binding.Exchange,
                                        routingKey  : bind.Binding.RoutingKey,
                                        noWait      : false,
                                        arguments   : bind.Binding.Arguments
                                    ),
                                    timeout   : bind.Timeout,
                                    promise   : promise
                                )
                            );

                            try {
                                await promise.Task;
                                bind.SetResult();
                            }
                            catch (Exception fault) {
                                bind.SetException(fault);
                            }
                            break;
                        }
                        case UnbindQueue unbind: {
                            var promise = new TaskCompletionSource();
                            context.Spawn(
                                RequestReplyProcessManager<QueueUnbind, QueueUnbindOk>.Create(
                                    channelId : state.ChannelId,
                                    dispatcher: state.Dispatcher,
                                    request   : new QueueUnbind(
                                        queueName   : unbind.Binding.Queue,
                                        exchangeName: unbind.Binding.Exchange,
                                        routingKey  : unbind.Binding.RoutingKey,
                                        arguments   : unbind.Binding.Arguments
                                    ),
                                    timeout   : unbind.Timeout,
                                    promise   : promise
                                )
                            );

                            try {
                                await promise.Task;
                                unbind.SetResult();
                            }
                            catch (Exception fault) {
                                unbind.SetException(fault);
                            }
                            break;
                        }
                        case PurgeQueue purge: {
                            var promise = new TaskCompletionSource();
                            context.Spawn(
                                RequestReplyProcessManager<QueuePurge, QueuePurgeOk>.Create(
                                    channelId : state.ChannelId,
                                    dispatcher: state.Dispatcher,
                                    request   : new QueuePurge(
                                        queueName: purge.Queue,
                                        noWait   : false
                                    ),
                                    timeout   : purge.Timeout,
                                    promise   : promise
                                )
                            );

                            try {
                                await promise.Task;
                                purge.SetResult();
                            }
                            catch (Exception fault) {
                                purge.SetException(fault);
                            }
                            break;
                        }
                        case Publish publish: {
                            var promise = new TaskCompletionSource();
                            context.Spawn(
                                PublishProcessManager.Create(
                                    channelId   : state.ChannelId,
                                    dispatcher  : state.Dispatcher,
                                    exchange    : publish.Exchange,
                                    routingKey  : publish.RoutingKey,
                                    routingFlags: publish.RoutingFlags,
                                    message     : publish.Message,
                                    maxFrameSize: _maxFrameSize,
                                    timeout     : publish.Timeout,
                                    promise     : promise
                                )
                            );

                            try {
                                await promise.Task;
                                publish.SetResult();
                            }
                            catch (Exception fault) {
                                publish.SetException(fault);
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
                            var promise = new TaskCompletionSource();
                            context.Spawn(
                                RequestReplyProcessManager<BasicQos, BasicQosOk>.Create(
                                    channelId : state.ChannelId,
                                    dispatcher: state.Dispatcher,
                                    request   : new BasicQos(
                                        prefetchSize : 0,
                                        prefetchCount: prefetch.Limit,
                                        global       : prefetch.Global
                                    ),
                                    timeout   : prefetch.Timeout,
                                    promise   : promise
                                )
                            );

                            try {
                                await promise.Task;
                                prefetch.SetResult();
                            }
                            catch (Exception fault) {
                                prefetch.SetException(fault);
                            }
                            break;
                        }
                        case Consume consume: {
                            var consumerTag = $"{Guid.NewGuid()}";
                            var promise = new TaskCompletionSource();
                            context.Spawn(
                                RequestReplyProcessManager<BasicConsume, BasicConsumeOk>.Create(
                                    channelId : state.ChannelId,
                                    dispatcher: state.Dispatcher,
                                    request   : new BasicConsume(
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
                                    ),
                                    timeout   : consume.Timeout,
                                    promise   : promise
                                )
                            );

                            try {
                                await promise.Task;

                                var consumer = context.SpawnNamed(
                                    name : $"consumer_{consumerTag}",
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
                            }
                            catch (Exception fault) {
                                consume.SetException(fault);
                            }
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
    }
}
