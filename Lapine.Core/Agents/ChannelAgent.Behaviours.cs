namespace Lapine.Agents;

using System.Collections.Immutable;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

static partial class ChannelAgent {
    static Behaviour<Protocol> Closed(UInt32 maxFrameSize) =>
        async context => {
            switch (context.Message) {
                case Open(var channelId, var receivedFrames, var connectionEvents, var socketAgent, var replyChannel, var cancellationToken): {
                    var dispatcher = DispatcherAgent.Create();
                    var consumers = ImmutableDictionary<String, IConsumerAgent>.Empty;
                    await dispatcher.DispatchTo(socketAgent, channelId);

                    var processManager = RequestReplyAgent<ChannelOpen, ChannelOpenOk>.StartNew(receivedFrames, dispatcher, cancellationToken);

                    switch (await processManager.Request(new ChannelOpen())) {
                        case Result<ChannelOpenOk>.Ok: {
                            replyChannel.Reply(true);
                            return context with { Behaviour = OpenBehaviour(maxFrameSize, receivedFrames, dispatcher, consumers) };
                        }
                        case Result<ChannelOpenOk>.Fault(var exceptionDispatchInfo): {
                            replyChannel.Reply(exceptionDispatchInfo.SourceException);
                            return context;
                        }
                    }
                    break;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Closed)}' behaviour.");
            }
            return context;
        };

    static Behaviour<Protocol> OpenBehaviour(UInt32 maxFrameSize, IObservable<RawFrame> receivedFrames, IDispatcherAgent dispatcher, IImmutableDictionary<String, IConsumerAgent> consumers, UInt64 deliveryTag = 1, Boolean enablePublisherConfirms = false) =>
        async context => {
            switch (context.Message) {
                case Close(var replyChannel, var cancellationToken): {
                    var processManager = RequestReplyAgent<ChannelClose, ChannelCloseOk>.StartNew(receivedFrames, dispatcher, cancellationToken);

                    switch (await processManager.Request(new ChannelClose(0, String.Empty, (0, 0)))) {
                        case Result<ChannelCloseOk>.Ok: {
                            replyChannel.Reply(true);
                            await context.Self.StopAsync();
                            break;
                        }
                        case Result<ChannelCloseOk>.Fault(var exceptionDispatchInfo): {
                            replyChannel.Reply(exceptionDispatchInfo.SourceException);
                            break;
                        }
                    }
                    return context;
                }
                case DeclareExchange(var exchange, var replyChannel, var cancellationToken): {
                    var processManager = RequestReplyAgent<ExchangeDeclare, ExchangeDeclareOk>.StartNew(receivedFrames, dispatcher, cancellationToken);

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

                    switch (await processManager.Request(command)) {
                        case Result<ExchangeDeclareOk>.Ok: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Result<ExchangeDeclareOk>.Fault(var exceptionDispatchInfo): {
                            replyChannel.Reply(exceptionDispatchInfo.SourceException);
                            break;
                        }
                    }
                    return context;
                }
                case DeleteExchange(var name, var condition, var replyChannel, var cancellationToken): {
                    var processManager = RequestReplyAgent<ExchangeDelete, ExchangeDeleteOk>.StartNew(receivedFrames, dispatcher, cancellationToken);

                    var command = new ExchangeDelete(
                        ExchangeName: name,
                        IfUnused    : condition.HasFlag(DeleteExchangeCondition.Unused),
                        NoWait      : false
                    );

                    switch (await processManager.Request(command)) {
                        case Result<ExchangeDeleteOk>.Ok: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Result<ExchangeDeleteOk>.Fault(var exceptionDispatchInfo): {
                            replyChannel.Reply(exceptionDispatchInfo.SourceException);
                            break;
                        }
                    }
                    return context;
                }
                case DeclareQueue(var queue, var replyChannel, var cancellationToken): {
                    var processManager = RequestReplyAgent<QueueDeclare, QueueDeclareOk>.StartNew(receivedFrames, dispatcher, cancellationToken);

                    var command = new QueueDeclare(
                        QueueName : queue.Name,
                        Passive   : false,
                        Durable   : queue.Durability == Durability.Durable,
                        Exclusive : queue.Exclusive,
                        AutoDelete: queue.AutoDelete,
                        NoWait    : false,
                        Arguments : queue.Arguments
                    );

                    switch (await processManager.Request(command)) {
                        case Result<QueueDeclareOk>.Ok: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Result<QueueDeclareOk>.Fault(var exceptionDispatchInfo): {
                            replyChannel.Reply(exceptionDispatchInfo.SourceException);
                            break;
                        }
                    }
                    return context;
                }
                case DeleteQueue(var name, var condition, var replyChannel, var cancellationToken): {
                    var processManager = RequestReplyAgent<QueueDelete, QueueDeleteOk>.StartNew(receivedFrames, dispatcher, cancellationToken);

                    var command = new QueueDelete(
                        QueueName: name,
                        IfUnused : condition.HasFlag(DeleteQueueCondition.Unused),
                        IfEmpty  : condition.HasFlag(DeleteQueueCondition.Empty),
                        NoWait   : false
                    );

                    switch (await processManager.Request(command)) {
                        case Result<QueueDeleteOk>.Ok: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Result<QueueDeleteOk>.Fault(var exceptionDispatchInfo): {
                            replyChannel.Reply(exceptionDispatchInfo.SourceException);
                            break;
                        }
                    }
                    return context;
                }
                case BindQueue(var binding, var replyChannel, var cancellationToken): {
                    var processManager = RequestReplyAgent<QueueBind, QueueBindOk>.StartNew(receivedFrames, dispatcher, cancellationToken);

                    var command = new QueueBind(
                        QueueName   : binding.Queue,
                        ExchangeName: binding.Exchange,
                        RoutingKey  : binding.RoutingKey,
                        NoWait      : false,
                        Arguments   : binding.Arguments
                    );

                    switch (await processManager.Request(command)) {
                        case Result<QueueBindOk>.Ok: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Result<QueueBindOk>.Fault(var exceptionDispatchInfo): {
                            replyChannel.Reply(exceptionDispatchInfo.SourceException);
                            break;
                        }
                    }
                    return context;
                }
                case UnbindQueue(var binding, var replyChannel, var cancellationToken): {
                    var processManager = RequestReplyAgent<QueueUnbind, QueueUnbindOk>.StartNew(receivedFrames, dispatcher, cancellationToken);

                    var command = new QueueUnbind(
                        QueueName   : binding.Queue,
                        ExchangeName: binding.Exchange,
                        RoutingKey  : binding.RoutingKey,
                        Arguments   : binding.Arguments
                    );

                    switch (await processManager.Request(command)) {
                        case Result<QueueUnbindOk>.Ok: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Result<QueueUnbindOk>.Fault(var exceptionDispatchInfo): {
                            replyChannel.Reply(exceptionDispatchInfo.SourceException);
                            break;
                        }
                    }
                    return context;
                }
                case PurgeQueue(var name, var replyChannel, var cancellationToken): {
                    var processManager = RequestReplyAgent<QueuePurge, QueuePurgeOk>.StartNew(receivedFrames, dispatcher, cancellationToken);

                    switch (await processManager.Request(new QueuePurge(name, NoWait: false))) {
                        case Result<QueuePurgeOk>.Ok(var queuePurgeOk): {
                            replyChannel.Reply(queuePurgeOk.MessageCount);
                            break;
                        }
                        case Result<QueuePurgeOk>.Fault(var exceptionDispatchInfo): {
                            replyChannel.Reply(exceptionDispatchInfo.SourceException);
                            break;
                        }
                    }
                    return context;
                }
                case Publish(var exchange, var routingKey, var routingFlags, var message, var replyChannel, var cancellationToken): {
                    var publishAgent = PublishAgent.Create(
                        receivedFrames          : receivedFrames,
                        dispatcher              : dispatcher,
                        maxFrameSize            : maxFrameSize,
                        publisherConfirmsEnabled: enablePublisherConfirms,
                        deliveryTag             : deliveryTag,
                        cancellationToken       : cancellationToken
                    );

                    switch (await publishAgent.Publish(exchange, routingKey, routingFlags, message)) {
                        case Result<Boolean>.Ok: {
                            replyChannel.Reply(true);
                            if (enablePublisherConfirms) {
                                deliveryTag += 1;
                            }

                            break;
                        }
                        case Result<Boolean>.Fault(var fault): {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    return context;
                }
                case GetMessage(var queue, var acknowledgements, var replyChannel, var cancellationToken): {
                    var processManager = GetMessageAgent.Create(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: cancellationToken
                    );

                    try {
                        switch (await processManager.GetMessages(queue, acknowledgements)) {
                            case NoMessage: {
                                replyChannel.Reply(null);
                                break;
                            }
                            case Message(var deliveryInfo, var properties, var body): {
                                replyChannel.Reply((deliveryInfo, properties, body));
                                break;
                            }
                        }
                    }
                    catch (Exception fault) {
                        replyChannel.Reply(fault);
                    }
                    return context;
                }
                case Acknowledge(var deliveryTag, var multiple, var replyChannel): {
                    await dispatcher.Dispatch(new BasicAck(
                        DeliveryTag: deliveryTag,
                        Multiple   : multiple
                    ));

                    replyChannel.Reply(true);
                    return context;
                }
                case Reject(var deliveryTag, var requeue, var replyChannel): {
                    await dispatcher.Dispatch(new BasicReject(
                        DeliveryTag: deliveryTag,
                        ReQueue    : requeue
                    ));

                    replyChannel.Reply(true);
                    return context;
                }
                case SetPrefetchLimit(var limit, var global, var replyChannel, var cancellationToken): {
                    var processManager = RequestReplyAgent<BasicQos, BasicQosOk>.StartNew(receivedFrames, dispatcher, cancellationToken);

                    var command = new BasicQos(
                        PrefetchSize : 0,
                        PrefetchCount: limit,
                        Global       : global
                    );

                    switch (await processManager.Request(command)) {
                        case Result<BasicQosOk>.Ok: {
                            replyChannel.Reply(true);
                            break;
                        }
                        case Result<BasicQosOk>.Fault(var exceptionDispatchInfo): {
                            replyChannel.Reply(exceptionDispatchInfo.SourceException);
                            break;
                        }
                    }
                    return context;
                }
                case Consume(var queue, var configuration, var arguments, var replyChannel, var cancellationToken): {
                    var consumerTag = $"{Guid.NewGuid()}";
                    var consumer = ConsumerAgent.Create();

                    switch (await consumer.StartConsuming(consumerTag, receivedFrames, dispatcher, queue, configuration, arguments)) {
                        case true: {
                            replyChannel.Reply(consumerTag);
                            return context with {
                                Behaviour = OpenBehaviour(maxFrameSize, receivedFrames, dispatcher, consumers.Add(consumerTag, consumer))
                            };
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }
                    return context;
                }
                case EnablePublisherConfirms(var replyChannel, var cancellationToken): {
                    var processManager = RequestReplyAgent<ConfirmSelect, ConfirmSelectOk>.StartNew(receivedFrames, dispatcher, cancellationToken);

                    switch (await processManager.Request(new ConfirmSelect(NoWait: false))) {
                        case Result<ConfirmSelectOk>.Ok: {
                            replyChannel.Reply(true);
                            enablePublisherConfirms = true;
                            break;
                        }
                        case Result<ConfirmSelectOk>.Fault(var exceptionDispatchInfo): {
                            replyChannel.Reply(exceptionDispatchInfo.SourceException);
                            break;
                        }
                    }
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Open)}' behaviour.");
            }
        };
}
