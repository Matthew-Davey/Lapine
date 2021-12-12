namespace Lapine.Agents;

using System.Collections.Immutable;
using Lapine.Agents.ProcessManagers;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;
using Proto;

using static Lapine.Agents.DispatcherAgent.Protocol;
using static Lapine.Agents.ChannelAgent.Protocol;
using static Lapine.Agents.ConsumerAgent.Protocol;

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
            Acknowledgements Acknowledgements,
            TimeSpan Timeout
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
        public record EnablePublisherConfirms(TimeSpan Timeout) : AsyncCommand;
    }

    static public Props Create(UInt32 maxFrameSize) =>
        Props.FromProducer(() => new Actor(maxFrameSize));

    readonly record struct State(
        UInt16 ChannelId,
        PID Dispatcher,
        IImmutableDictionary<String, PID> Consumers,
        Boolean PublisherConfirmsEnabled = false,
        UInt64 DeliveryTag = 1
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
                    var state = new State(
                        ChannelId : open.ChannelId,
                        Dispatcher: context.SpawnNamed(
                            name : "dispatcher",
                            props: DispatcherAgent.Create()
                        ),
                        Consumers : ImmutableDictionary<String, PID>.Empty
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
                    await promise.Task.ContinueWith(
                        onCompleted: () => {
                            open.SetResult();
                            _behaviour.Become(Open(state));
                        },
                        onFaulted: open.SetException
                    );
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
                        await promise.Task.ContinueWith(
                            onCompleted: () => {
                                close.SetResult();
                                context.Stop(context.Self!);
                            },
                            onFaulted: close.SetException
                        );
                        break;
                    }
                    case DeclareExchange declare: {
                        var promise = new TaskCompletionSource();
                        context.Spawn(
                            RequestReplyProcessManager<ExchangeDeclare, ExchangeDeclareOk>.Create(
                                channelId : state.ChannelId,
                                dispatcher: state.Dispatcher,
                                request: new ExchangeDeclare(
                                    ExchangeName: declare.Definition.Name,
                                    ExchangeType: declare.Definition.Type,
                                    Passive     : false,
                                    Durable     : declare.Definition.Durability == Durability.Durable,
                                    AutoDelete  : declare.Definition.AutoDelete,
                                    Internal    : false,
                                    NoWait      : false,
                                    Arguments   : declare.Definition.Arguments
                                ),
                                timeout   : declare.Timeout,
                                promise   : promise
                            )
                        );
                        await promise.Task.ContinueWith(
                            onCompleted: declare.SetResult,
                            onFaulted  : declare.SetException
                        );
                        break;
                    }
                    case DeleteExchange delete: {
                        var promise = new TaskCompletionSource();
                        context.Spawn(
                            RequestReplyProcessManager<ExchangeDelete, ExchangeDeleteOk>.Create(
                                channelId : state.ChannelId,
                                dispatcher: state.Dispatcher,
                                request: new ExchangeDelete(
                                    ExchangeName: delete.Exchange,
                                    IfUnused    : delete.Condition.HasFlag(DeleteExchangeCondition.Unused),
                                    NoWait      : false
                                ),
                                timeout   : delete.Timeout,
                                promise   : promise
                            )
                        );
                        await promise.Task.ContinueWith(
                            onCompleted: delete.SetResult,
                            onFaulted  : delete.SetException
                        );
                        break;
                    }
                    case DeclareQueue declare: {
                        var promise = new TaskCompletionSource();
                        context.Spawn(
                            RequestReplyProcessManager<QueueDeclare, QueueDeclareOk>.Create(
                                channelId : state.ChannelId,
                                dispatcher: state.Dispatcher,
                                request   : new QueueDeclare(
                                    QueueName : declare.Definition.Name,
                                    Passive   : false,
                                    Durable   : declare.Definition.Durability == Durability.Durable,
                                    Exclusive : declare.Definition.Exclusive,
                                    AutoDelete: declare.Definition.AutoDelete,
                                    NoWait    : false,
                                    Arguments : declare.Definition.Arguments
                                ),
                                timeout   : declare.Timeout,
                                promise   : promise
                            )
                        );
                        await promise.Task.ContinueWith(
                            onCompleted: declare.SetResult,
                            onFaulted  : declare.SetException
                        );
                        break;
                    }
                    case DeleteQueue delete: {
                        var promise = new TaskCompletionSource();
                        context.Spawn(
                            RequestReplyProcessManager<QueueDelete, QueueDeleteOk>.Create(
                                channelId : state.ChannelId,
                                dispatcher: state.Dispatcher,
                                request   : new QueueDelete(
                                    QueueName: delete.Queue,
                                    IfUnused : delete.Condition.HasFlag(DeleteQueueCondition.Unused),
                                    IfEmpty  : delete.Condition.HasFlag(DeleteQueueCondition.Empty),
                                    NoWait   : false
                                ),
                                timeout   : delete.Timeout,
                                promise   : promise
                            )
                        );
                        await promise.Task.ContinueWith(
                            onCompleted: delete.SetResult,
                            onFaulted  : delete.SetException
                        );
                        break;
                    }
                    case BindQueue bind: {
                        var promise = new TaskCompletionSource();
                        context.Spawn(
                            RequestReplyProcessManager<QueueBind, QueueBindOk>.Create(
                                channelId : state.ChannelId,
                                dispatcher: state.Dispatcher,
                                request   : new QueueBind(
                                    QueueName   : bind.Binding.Queue,
                                    ExchangeName: bind.Binding.Exchange,
                                    RoutingKey  : bind.Binding.RoutingKey,
                                    NoWait      : false,
                                    Arguments   : bind.Binding.Arguments
                                ),
                                timeout   : bind.Timeout,
                                promise   : promise
                            )
                        );
                        await promise.Task.ContinueWith(
                            onCompleted: bind.SetResult,
                            onFaulted  : bind.SetException
                        );
                        break;
                    }
                    case UnbindQueue unbind: {
                        var promise = new TaskCompletionSource();
                        context.Spawn(
                            RequestReplyProcessManager<QueueUnbind, QueueUnbindOk>.Create(
                                channelId : state.ChannelId,
                                dispatcher: state.Dispatcher,
                                request   : new QueueUnbind(
                                    QueueName   : unbind.Binding.Queue,
                                    ExchangeName: unbind.Binding.Exchange,
                                    RoutingKey  : unbind.Binding.RoutingKey,
                                    Arguments   : unbind.Binding.Arguments
                                ),
                                timeout   : unbind.Timeout,
                                promise   : promise
                            )
                        );
                        await promise.Task.ContinueWith(
                            onCompleted: unbind.SetResult,
                            onFaulted  : unbind.SetException
                        );
                        break;
                    }
                    case PurgeQueue purge: {
                        var promise = new TaskCompletionSource();
                        context.Spawn(
                            RequestReplyProcessManager<QueuePurge, QueuePurgeOk>.Create(
                                channelId : state.ChannelId,
                                dispatcher: state.Dispatcher,
                                request   : new QueuePurge(
                                    QueueName: purge.Queue,
                                    NoWait   : false
                                ),
                                timeout   : purge.Timeout,
                                promise   : promise
                            )
                        );
                        await promise.Task.ContinueWith(
                            onCompleted: purge.SetResult,
                            onFaulted  : purge.SetException
                        );
                        break;
                    }
                    case Publish publish: {
                        var promise = new TaskCompletionSource();
                        context.Spawn(
                            PublishProcessManager.Create(
                                channelId               : state.ChannelId,
                                dispatcher              : state.Dispatcher,
                                exchange                : publish.Exchange,
                                routingKey              : publish.RoutingKey,
                                routingFlags            : publish.RoutingFlags,
                                message                 : publish.Message,
                                maxFrameSize            : _maxFrameSize,
                                publisherConfirmsEnabled: state.PublisherConfirmsEnabled,
                                deliveryTag             : state.DeliveryTag,
                                timeout                 : publish.Timeout,
                                promise                 : promise
                            )
                        );
                        await promise.Task.ContinueWith(
                            onCompleted: () => {
                                publish.SetResult();
                                if (state.PublisherConfirmsEnabled) {
                                    state = state with {
                                        DeliveryTag = state.DeliveryTag +1
                                    };
                                }
                            },
                            onFaulted: publish.SetException
                        );
                        break;
                    }
                    case GetMessage get: {
                        var promise = new TaskCompletionSource<(DeliveryInfo, BasicProperties, ReadOnlyMemory<Byte>)?>();
                        context.Spawn(
                            GetMessageProcessManager.Create(
                                channelId       : state.ChannelId,
                                dispatcher      : state.Dispatcher,
                                queue           : get.Queue,
                                acknowledgements: get.Acknowledgements,
                                timeout         : get.Timeout,
                                promise         : promise
                            )
                        );
                        await promise.Task.ContinueWith(
                            onCompleted: get.SetResult,
                            onFaulted  : get.SetException
                        );
                        break;
                    }
                    case Acknowledge ack: {
                        context.Send(state.Dispatcher, Dispatch.Command(new BasicAck(
                            DeliveryTag: ack.DeliveryTag,
                            Multiple   : ack.Multiple
                        )));
                        ack.SetResult();
                        break;
                    }
                    case Reject reject: {
                        context.Send(state.Dispatcher, Dispatch.Command(new BasicReject(
                            DeliveryTag: reject.DeliveryTag,
                            ReQueue    : reject.Requeue
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
                                    PrefetchSize : 0,
                                    PrefetchCount: prefetch.Limit,
                                    Global       : prefetch.Global
                                ),
                                timeout   : prefetch.Timeout,
                                promise   : promise
                            )
                        );
                        await promise.Task.ContinueWith(
                            onCompleted: prefetch.SetResult,
                            onFaulted  : prefetch.SetException
                        );
                        break;
                    }
                    case Consume consume: {
                        var consumerTag = $"{Guid.NewGuid()}";
                        var consumer = context.SpawnNamed(
                            name : $"consume_{consumerTag}",
                            props: ConsumerAgent.Create()
                        );
                        var promise = new TaskCompletionSource();
                        context.Send(consumer, new StartConsuming(
                            ChannelId            : state.ChannelId,
                            ConsumerTag          : consumerTag,
                            Dispatcher           : state.Dispatcher,
                            Queue                : consume.Queue,
                            ConsumerConfiguration: consume.ConsumerConfiguration,
                            Arguments            : consume.Arguments,
                            Promise              : promise
                        ));
                        await promise.Task.ContinueWith(
                            onCompleted: () => {
                                _behaviour.Become(Open(state with {
                                    Consumers = state.Consumers.Add(consumerTag, consumer)
                                }));
                                consume.SetResult(consumerTag);
                            },
                            onFaulted: consume.SetException
                        );
                        break;
                    }
                    case EnablePublisherConfirms command: {
                        var promise = new TaskCompletionSource();
                        context.Spawn(
                            RequestReplyProcessManager<ConfirmSelect, ConfirmSelectOk>.Create(
                                channelId : state.ChannelId,
                                dispatcher: state.Dispatcher,
                                request   : new ConfirmSelect(
                                    NoWait: false
                                ),
                                timeout   : command.Timeout,
                                promise   : promise
                            )
                        );
                        await promise.Task.ContinueWith(
                            onCompleted: () => {
                                command.SetResult();
                                _behaviour.Become(Open(state with {
                                    PublisherConfirmsEnabled = true
                                }));
                            },
                            onFaulted: command.SetException
                        );
                        break;
                    }
                }
            };
    }
}
