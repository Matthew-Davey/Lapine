namespace Lapine.Agents;

using System.Collections.Immutable;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

using static Lapine.Agents.ConsumerAgent.Protocol;
using static Lapine.Agents.MessageAssemblerAgent.Protocol;
using static Lapine.Agents.MessageHandlerAgent.Protocol;

static class ConsumerAgent {
    static public class Protocol {
        public record StartConsuming(
            String ConsumerTag,
            IObservable<RawFrame> ReceivedFrames,
            IAgent Dispatcher,
            String Queue,
            ConsumerConfiguration ConsumerConfiguration,
            IReadOnlyDictionary<String, Object>? Arguments
        );
        public record ConsumeMessage(
            DeliveryInfo Delivery,
            BasicProperties Properties,
            MemoryBufferWriter<Byte> Buffer
        );
    }

    static public IAgent Create() =>
        Agent.StartNew(Unstarted());

    readonly record struct Message(
        DeliveryInfo DeliveryInfo,
        BasicProperties Properties,
        MemoryBufferWriter<Byte> Body
    );

    static Behaviour Unstarted() =>
        async context => {
            switch (context.Message) {
                case (StartConsuming start, AsyncReplyChannel replyChannel): {
                    var handlers = Enumerable.Range(0, start.ConsumerConfiguration.MaxDegreeOfParallelism)
                        .Select(_ => MessageHandlerAgent.Create(context.Self))
                        .ToImmutableQueue();
                    var assembler = MessageAssemblerAgent.StartNew();
                    await assembler.PostAsync(new Begin(start.ReceivedFrames, context.Self));
                    var processManager = RequestReplyAgent.StartNew<BasicConsume, BasicConsumeOk>(
                        receivedFrames   : start.ReceivedFrames,
                        dispatcher       : start.Dispatcher,
                        cancellationToken: CancellationToken.None
                    );

                    var command = new BasicConsume(
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

                    switch (await processManager.PostAndReplyAsync(command)) {
                        case BasicConsumeOk: {
                            replyChannel.Reply(true);

                            return context with { Behaviour = Running(
                                consumerTag          : start.ConsumerTag,
                                receivedFrames       : start.ReceivedFrames,
                                dispatcher           : start.Dispatcher,
                                consumerConfiguration: start.ConsumerConfiguration,
                                availableHandlers    : handlers,
                                busyHandlers         : ImmutableList<IAgent>.Empty,
                                inbox                : ImmutableQueue<Message>.Empty
                            ) };
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }

                    break;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Unstarted)}' behaviour.");
            }
            return context;
        };

    static Behaviour Running(String consumerTag, IObservable<RawFrame> receivedFrames, IAgent dispatcher, ConsumerConfiguration consumerConfiguration, IImmutableQueue<IAgent> availableHandlers, IImmutableList<IAgent> busyHandlers, IImmutableQueue<Message> inbox) =>
        async context => {
            switch (context.Message) {
                case ConsumeMessage(var deliveryInfo, var properties, var buffer) when availableHandlers.Any(): {
                    availableHandlers = availableHandlers.Dequeue(out var handler);
                    await handler.PostAsync(new HandleMessage(
                        Dispatcher           : dispatcher,
                        ConsumerConfiguration: consumerConfiguration,
                        Delivery             : deliveryInfo,
                        Properties           : properties,
                        Buffer               : buffer
                    ));

                    return context with { Behaviour = Running(
                        consumerTag          : consumerTag,
                        receivedFrames       : receivedFrames,
                        dispatcher           : dispatcher,
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
                        consumerConfiguration: consumerConfiguration,
                        availableHandlers    : availableHandlers.Enqueue(handler),
                        busyHandlers         : busyHandlers.Remove(handler),
                        inbox                : inbox
                    ) };
                }
                case HandlerReady(var handler) when inbox.Any(): {
                    inbox = inbox.Dequeue(out var message);

                    await handler.PostAsync(new HandleMessage(
                        Dispatcher           : dispatcher,
                        ConsumerConfiguration: consumerConfiguration,
                        Delivery             : message.DeliveryInfo,
                        Properties           : message.Properties,
                        Buffer               : message.Body
                    ));

                    return context with { Behaviour = Running(
                        consumerTag          : consumerTag,
                        receivedFrames       : receivedFrames,
                        dispatcher           : dispatcher,
                        consumerConfiguration: consumerConfiguration,
                        availableHandlers    : availableHandlers,
                        busyHandlers         : busyHandlers,
                        inbox                : inbox
                    ) };
                }
                case Stopped: {
                    var processManager = RequestReplyAgent.StartNew<BasicCancel, BasicCancelOk>(
                        receivedFrames   : receivedFrames,
                        dispatcher       : dispatcher,
                        cancellationToken: CancellationToken.None
                    );

                    switch (await processManager.PostAndReplyAsync(new BasicCancel(consumerTag, false))) {
                        case BasicCancelOk: {
                            foreach (var handlerAgent in availableHandlers)
                                await handlerAgent.StopAsync();

                            foreach (var handlerAgent in busyHandlers)
                                await handlerAgent.StopAsync();

                            return context;
                        }
                    }
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Running)}' behaviour.");
            }
        };
}
