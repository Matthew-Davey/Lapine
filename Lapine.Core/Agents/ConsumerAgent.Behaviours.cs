namespace Lapine.Agents;

using System.Collections.Immutable;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

static partial class ConsumerAgent {
    readonly record struct Message(
        DeliveryInfo DeliveryInfo,
        BasicProperties Properties,
        MemoryBufferWriter<Byte> Body
    );

    static Behaviour<Protocol> Unstarted() =>
        async context => {
            switch (context.Message) {
                case StartConsuming start: {
                    var processManager = RequestReplyAgent<BasicConsume, BasicConsumeOk>.StartNew(start.ReceivedFrames, start.Dispatcher, CancellationToken.None);

                    var basicConsume = new BasicConsume(
                        QueueName  : start.Queue,
                        ConsumerTag: start.ConsumerTag,
                        NoLocal    : false,
                        NoAck      : start.ConsumerConfiguration.Acknowledgements switch {
                            Acknowledgements.Auto => true,
                            _ => false
                        },
                        Exclusive: start.ConsumerConfiguration.Exclusive,
                        NoWait   : false,
                        Arguments: start.Arguments ?? ImmutableDictionary<String, Object>.Empty
                    );

                    switch (await processManager.Request(basicConsume)) {
                        case Result<BasicConsumeOk>.Ok(var basicConsumeOk): {
                            var handlers = Enumerable.Range(0, start.ConsumerConfiguration.MaxDegreeOfParallelism)
                                .Select(_ => MessageHandlerAgent.Create(start.Self))
                                .ToImmutableQueue();
                            var assembler = MessageAssemblerAgent.StartNew();
                            var receivedMessages = await assembler.Begin(start.ReceivedFrames, start.Self);
                            receivedMessages.Subscribe(async message => await context.Self.PostAsync(new ConsumeMessage(message.DeliveryInfo, message.Properties, message.Buffer)));

                            start.ReplyChannel.Reply(true);

                            return context with { Behaviour = Running(
                                consumerTag          : basicConsumeOk.ConsumerTag,
                                receivedFrames       : start.ReceivedFrames,
                                dispatcher           : start.Dispatcher,
                                assembler            : assembler,
                                consumerConfiguration: start.ConsumerConfiguration,
                                availableHandlers    : handlers,
                                busyHandlers         : ImmutableList<IMessageHandlerAgent>.Empty,
                                inbox                : ImmutableQueue<Message>.Empty
                            ) };
                        }
                        case Result<BasicConsumeOk>.Fault(var exceptionDispatchInfo): {
                            start.ReplyChannel.Reply(exceptionDispatchInfo.SourceException);
                            break;
                        }
                    }
                    break;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Unstarted)}' behaviour.");
            }
            return context;
        };

    static Behaviour<Protocol> Running(String consumerTag, IObservable<RawFrame> receivedFrames, IDispatcherAgent dispatcher, IMessageAssemblerAgent assembler, ConsumerConfiguration consumerConfiguration, IImmutableQueue<IMessageHandlerAgent> availableHandlers, IImmutableList<IMessageHandlerAgent> busyHandlers, IImmutableQueue<Message> inbox) =>
        async context => {
            switch (context.Message) {
                case ConsumeMessage(var deliveryInfo, var properties, var buffer) when availableHandlers.Any(): {
                    availableHandlers = availableHandlers.Dequeue(out var handler);
                    await handler.HandleMessage(dispatcher, consumerConfiguration, deliveryInfo, properties, buffer);

                    return context with { Behaviour = Running(
                        consumerTag          : consumerTag,
                        receivedFrames       : receivedFrames,
                        dispatcher           : dispatcher,
                        assembler            : assembler,
                        consumerConfiguration: consumerConfiguration,
                        availableHandlers    : availableHandlers,
                        busyHandlers         : busyHandlers.Add(handler),
                        inbox                : inbox
                    ) };
                }
                case ConsumeMessage(var deliveryInfo, var properties, var buffer) when availableHandlers.IsEmpty: {
                    return context with { Behaviour = Running(
                        consumerTag          : consumerTag,
                        receivedFrames       : receivedFrames,
                        dispatcher           : dispatcher,
                        assembler            : assembler,
                        consumerConfiguration: consumerConfiguration,
                        availableHandlers    : availableHandlers,
                        busyHandlers         : busyHandlers,
                        inbox                : inbox.Enqueue(new Message(
                            DeliveryInfo: deliveryInfo,
                            Properties  : properties,
                            Body        : buffer
                        ))
                    ) };
                }
                case HandlerReady(var handler) when inbox.IsEmpty: {
                    return context with { Behaviour = Running(
                        consumerTag          : consumerTag,
                        receivedFrames       : receivedFrames,
                        dispatcher           : dispatcher,
                        assembler            : assembler,
                        consumerConfiguration: consumerConfiguration,
                        availableHandlers    : availableHandlers.Enqueue(handler),
                        busyHandlers         : busyHandlers.Remove(handler),
                        inbox                : inbox
                    ) };
                }
                case HandlerReady(var handler) when inbox.Any(): {
                    inbox = inbox.Dequeue(out var message);

                    await handler.HandleMessage(dispatcher, consumerConfiguration, message.DeliveryInfo, message.Properties, message.Body);

                    return context with { Behaviour = Running(
                        consumerTag          : consumerTag,
                        receivedFrames       : receivedFrames,
                        dispatcher           : dispatcher,
                        assembler            : assembler,
                        consumerConfiguration: consumerConfiguration,
                        availableHandlers    : availableHandlers,
                        busyHandlers         : busyHandlers,
                        inbox                : inbox
                    ) };
                }
                case Stop: {
                    var processManager = RequestReplyAgent<BasicCancel, BasicCancelOk>.StartNew(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: CancellationToken.None
                    );

                    switch (await processManager.Request(new BasicCancel(consumerTag, false))) {
                        case Result<BasicCancelOk>.Ok(var basicCancelOk): {
                            foreach (var handlerAgent in availableHandlers)
                                await handlerAgent.Stop();

                            foreach (var handlerAgent in busyHandlers)
                                await handlerAgent.Stop();

                            await assembler.Stop();

                            await context.Self.StopAsync();
                            return context;
                        }
                        case Result<BasicCancelOk>.Fault(var exceptionDispatchInfo): {
                            // TODO
                            break;
                        }
                    }
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Running)}' behaviour.");
            }
        };
}
