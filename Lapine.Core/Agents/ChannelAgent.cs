namespace Lapine.Agents;

using System.Collections.Immutable;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

using static Lapine.Agents.DispatcherAgent.Protocol;
using static Lapine.Agents.ChannelAgent.Protocol;
using static Lapine.Agents.ConsumerAgent.Protocol;
using static Lapine.Agents.PublishAgent.Protocol;

static class ChannelAgent {
    static public class Protocol {
        public record Open(UInt16 ChannelId, IObservable<RawFrame> ReceivedFrames, IObservable<Object> ConnectionEvents, IAgent SocketAgent, CancellationToken CancellationToken = default);
        public record Close(CancellationToken CancellationToken = default);
        public record DeclareExchange(ExchangeDefinition Definition, CancellationToken CancellationToken = default);
        public record DeleteExchange(String Exchange, DeleteExchangeCondition Condition, CancellationToken CancellationToken = default);
        public record DeclareQueue(QueueDefinition Definition, CancellationToken CancellationToken = default);
        public record DeleteQueue(String Queue, DeleteQueueCondition Condition, CancellationToken CancellationToken = default);
        public record BindQueue(Binding Binding, CancellationToken CancellationToken = default);
        public record UnbindQueue(Binding Binding, CancellationToken CancellationToken = default);
        public record PurgeQueue(String Queue, CancellationToken CancellationToken = default);
        public record Publish(
            String Exchange,
            String RoutingKey,
            RoutingFlags RoutingFlags,
            (BasicProperties Properties, ReadOnlyMemory<Byte> Body) Message,
            CancellationToken CancellationToken
        );
        public record GetMessage(
            String Queue,
            Acknowledgements Acknowledgements,
            CancellationToken CancellationToken = default
        );
        public record Acknowledge(UInt64 DeliveryTag, Boolean Multiple);
        public record Reject(UInt64 DeliveryTag, Boolean Requeue);
        public record SetPrefetchLimit(UInt16 Limit, Boolean Global, CancellationToken CancellationToken = default);
        public record Consume(
            String Queue,
            ConsumerConfiguration ConsumerConfiguration,
            IReadOnlyDictionary<String, Object>? Arguments,
            CancellationToken CancellationToken = default
        );
        public record EnablePublisherConfirms(CancellationToken CancellationToken = default);
    }

    static public IAgent Create(UInt32 maxFrameSize) =>
        Agent.StartNew(Closed(maxFrameSize));

    static Behaviour Closed(UInt32 maxFrameSize) =>
        async context => {
            switch (context.Message) {
                case (Open(var channelId, var receivedFrames, var connectionEvents, var socketAgent, var cancellationToken), AsyncReplyChannel replyChannel): {
                    var dispatcher = DispatcherAgent.Create();
                    var consumers = ImmutableDictionary<String, IAgent>.Empty;
                    await dispatcher.PostAsync(new DispatchTo(socketAgent, channelId));

                    var processManager = RequestReplyAgent.StartNew<ChannelOpen, ChannelOpenOk>(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: cancellationToken
                    );

                    switch (await processManager.PostAndReplyAsync(new ChannelOpen())) {
                        case ChannelOpenOk: {
                            replyChannel.Reply(true);
                            return context with { Behaviour = Open(maxFrameSize, receivedFrames, dispatcher, consumers) };
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    break;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Closed)}' behaviour.");
            }
            return context;
        };

    static Behaviour Open(UInt32 maxFrameSize, IObservable<RawFrame> receivedFrames, IAgent dispatcher, IImmutableDictionary<String, IAgent> consumers, UInt64 deliveryTag = 1, Boolean enablePublisherConfirms = false) =>
        async context => {
            switch (context.Message) {
                case (Close(var cancellationToken), AsyncReplyChannel replyChannel): {
                    var processManager = RequestReplyAgent.StartNew<ChannelClose, ChannelCloseOk>(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: cancellationToken
                    );

                    switch (await processManager.PostAndReplyAsync(new ChannelClose(0, String.Empty, (0, 0)))) {
                        case ChannelCloseOk: {
                            replyChannel.Reply(true);
                            await context.Self.StopAsync();
                            break;
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    return context;
                }
                case (DeclareExchange(var exchange, var cancellationToken), AsyncReplyChannel replyChannel): {
                    var processManager = RequestReplyAgent.StartNew<ExchangeDeclare, ExchangeDeclareOk>(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: cancellationToken
                    );

                    var command = new ExchangeDeclare(
                        ExchangeName: exchange.Name,
                        ExchangeType: exchange.Type,
                        Passive     : false,
                        Durable     : exchange.Durability == Durability.Durable,
                        AutoDelete  : exchange.AutoDelete,
                        Internal    : false,
                        NoWait      : false,
                        Arguments   : exchange.Arguments
                    );

                    switch (await processManager.PostAndReplyAsync(command)) {
                        case ExchangeDeclareOk: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    return context;
                }
                case (DeleteExchange(var name, var condition, var cancellationToken), AsyncReplyChannel replyChannel): {
                    var processManager = RequestReplyAgent.StartNew<ExchangeDelete, ExchangeDeleteOk>(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: cancellationToken
                    );

                    var command = new ExchangeDelete(
                        ExchangeName: name,
                        IfUnused    : condition.HasFlag(DeleteExchangeCondition.Unused),
                        NoWait      : false
                    );

                    switch (await processManager.PostAndReplyAsync(command)) {
                        case ExchangeDeleteOk: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    return context;
                }
                case (DeclareQueue(var queue, var cancellationToken), AsyncReplyChannel replyChannel): {
                    var processManager = RequestReplyAgent.StartNew<QueueDeclare, QueueDeclareOk>(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: cancellationToken
                    );

                    var command = new QueueDeclare(
                        QueueName : queue.Name,
                        Passive   : false,
                        Durable   : queue.Durability == Durability.Durable,
                        Exclusive : queue.Exclusive,
                        AutoDelete: queue.AutoDelete,
                        NoWait    : false,
                        Arguments : queue.Arguments
                    );

                    switch (await processManager.PostAndReplyAsync(command)) {
                        case QueueDeclareOk: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    return context;
                }
                case (DeleteQueue(var name, var condition, var cancellationToken), AsyncReplyChannel replyChannel): {
                    var processManager = RequestReplyAgent.StartNew<QueueDelete, QueueDeleteOk>(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: cancellationToken
                    );

                    var command = new QueueDelete(
                        QueueName: name,
                        IfUnused : condition.HasFlag(DeleteQueueCondition.Unused),
                        IfEmpty  : condition.HasFlag(DeleteQueueCondition.Empty),
                        NoWait   : false
                    );

                    switch (await processManager.PostAndReplyAsync(command)) {
                        case QueueDeleteOk: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    return context;
                }
                case (BindQueue(var binding, var cancellationToken), AsyncReplyChannel replyChannel): {
                    var processManager = RequestReplyAgent.StartNew<QueueBind, QueueBindOk>(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: cancellationToken
                    );

                    var command = new QueueBind(
                        QueueName   : binding.Queue,
                        ExchangeName: binding.Exchange,
                        RoutingKey  : binding.RoutingKey,
                        NoWait      : false,
                        Arguments   : binding.Arguments
                    );

                    switch (await processManager.PostAndReplyAsync(command)) {
                        case QueueBindOk: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    return context;
                }
                case (UnbindQueue(var binding, var cancellationToken), AsyncReplyChannel replyChannel): {
                    var processManager = RequestReplyAgent.StartNew<QueueUnbind, QueueUnbindOk>(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: cancellationToken
                    );

                    var command = new QueueUnbind(
                        QueueName   : binding.Queue,
                        ExchangeName: binding.Exchange,
                        RoutingKey  : binding.RoutingKey,
                        Arguments   : binding.Arguments
                    );

                    switch (await processManager.PostAndReplyAsync(command)) {
                        case QueueUnbindOk: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    return context;
                }
                case (PurgeQueue(var name, var cancellationToken), AsyncReplyChannel replyChannel): {
                    var processManager = RequestReplyAgent.StartNew<QueuePurge, QueuePurgeOk>(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: cancellationToken
                    );

                    switch (await processManager.PostAndReplyAsync(new QueuePurge(name, NoWait: false))) {
                        case QueuePurgeOk: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    return context;
                }
                case (Publish(var exchange, var routingKey, var routingFlags, var message, var cancellationToken), AsyncReplyChannel replyChannel): {
                    var publishAgent = PublishAgent.Create(
                        receivedFrames          : receivedFrames,
                        dispatcher              : dispatcher,
                        maxFrameSize            : maxFrameSize,
                        publisherConfirmsEnabled: enablePublisherConfirms,
                        deliveryTag             : deliveryTag,
                        cancellationToken       : cancellationToken
                    );

                    var command = new PublishMessage(exchange, routingKey, routingFlags, message);

                    switch (await publishAgent.PostAndReplyAsync(command)) {
                        case true: {
                            replyChannel.Reply(true);
                            if (enablePublisherConfirms) {
                                deliveryTag += 1;
                            }

                            break;
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    return context;
                }
                case (GetMessage(var queue, var acknowledgements, var cancellationToken), AsyncReplyChannel replyChannel): {
                    var processManager = GetMessageAgent.Create(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: cancellationToken
                    );

                    var command = new GetMessageAgent.Protocol.GetMessages(queue, acknowledgements);

                    switch (await processManager.PostAndReplyAsync(command))
                    {
                        case GetMessageAgent.Protocol.NoMessages: {
                            replyChannel.Reply(null);
                            break;
                        }
                        case (DeliveryInfo deliveryInfo, BasicProperties properties, ReadOnlyMemory<Byte> body): {
                            replyChannel.Reply((deliveryInfo, properties, body));
                            break;
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    };
                    return context;
                }
                case (Acknowledge(var deliveryTag, var multiple), AsyncReplyChannel replyChannel): {
                    var command = new BasicAck(
                        DeliveryTag: deliveryTag,
                        Multiple   : multiple
                    );

                    await dispatcher.PostAsync(Dispatch.Command(command));

                    replyChannel.Reply(true);
                    return context;
                }
                case (Reject(var deliveryTag, var requeue), AsyncReplyChannel replyChannel): {
                    var command = new BasicReject(
                        DeliveryTag: deliveryTag,
                        ReQueue    : requeue
                    );

                    await dispatcher.PostAsync(Dispatch.Command(command));

                    replyChannel.Reply(true);
                    return context;
                }
                case (SetPrefetchLimit(var limit, var global, var cancellationToken), AsyncReplyChannel replyChannel): {
                    var processManager = RequestReplyAgent.StartNew<BasicQos, BasicQosOk>(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: cancellationToken
                    );

                    var command = new BasicQos(
                        PrefetchSize : 0,
                        PrefetchCount: limit,
                        Global       : global
                    );

                    switch (await processManager.PostAndReplyAsync(command)) {
                        case BasicQosOk: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    return context;
                }
                case (Consume(var queue, var configuration, var arguments, var cancellationToken), AsyncReplyChannel replyChannel): {
                    var consumerTag = $"{Guid.NewGuid()}";
                    var consumer = ConsumerAgent.Create();

                    var command = new StartConsuming(
                        ConsumerTag          : consumerTag,
                        ReceivedFrames       : receivedFrames,
                        Dispatcher           : dispatcher,
                        Queue                : queue,
                        ConsumerConfiguration: configuration,
                        Arguments            : arguments
                    );

                    switch (await consumer.PostAndReplyAsync(command)) {
                        case true: {
                            replyChannel.Reply(consumerTag);
                            return context with {
                                Behaviour = Open(maxFrameSize, receivedFrames, dispatcher, consumers.Add(consumerTag, consumer))
                            };
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    return context;
                }
                case (EnablePublisherConfirms(var cancellationToken), AsyncReplyChannel replyChannel): {
                    var processManager = RequestReplyAgent.StartNew<ConfirmSelect, ConfirmSelectOk>(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: cancellationToken
                    );

                    switch (await processManager.PostAndReplyAsync(new ConfirmSelect(NoWait: false))) {
                        case ConfirmSelectOk: {
                            replyChannel.Reply(true);
                            enablePublisherConfirms = true;
                            break;
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Open)}' behaviour.");
            }
        };
}
