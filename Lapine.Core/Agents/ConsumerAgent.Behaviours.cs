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

    record State(
        String ConsumerTag,
        IObservable<RawFrame> ReceivedFrames,
        IDispatcherAgent Dispatcher,
        IMessageAssemblerAgent Assembler,
        ConsumerConfiguration ConsumerConfiguration,
        IImmutableQueue<IMessageHandlerAgent> AvailableHandlers,
        IImmutableList<IMessageHandlerAgent> BusyHandlers,
        IImmutableQueue<Message> Inbox
    );

    static Behaviour<Protocol> Unstarted() =>
        async context => {
            switch (context.Message) {
                case StartConsuming start: {
                    var processManager = RequestReplyAgent<BasicConsume, BasicConsumeOk>.StartNew(start.ReceivedFrames, start.Dispatcher);

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

                    return await processManager.Request(basicConsume)
                        .ContinueWith(
                            onCompleted: async basicConsumeOk => {
                                var handlers = Enumerable.Range(0, start.ConsumerConfiguration.MaxDegreeOfParallelism)
                                    .Select(_ => MessageHandlerAgent.Create(start.Self))
                                    .ToImmutableQueue();
                                var assembler = MessageAssemblerAgent.StartNew();
                                var receivedMessages = await assembler.Begin(start.ReceivedFrames, start.Self);
                                receivedMessages.Subscribe(async message => await context.Self.PostAsync(new ConsumeMessage(message.DeliveryInfo, message.Properties, message.Buffer)));

                                start.ReplyChannel.Complete();

                                var state = new State(
                                    ConsumerTag          : basicConsumeOk.ConsumerTag,
                                    ReceivedFrames       : start.ReceivedFrames,
                                    Dispatcher           : start.Dispatcher,
                                    Assembler            : assembler,
                                    ConsumerConfiguration: start.ConsumerConfiguration,
                                    AvailableHandlers    : handlers,
                                    BusyHandlers         : ImmutableList<IMessageHandlerAgent>.Empty,
                                    Inbox                : ImmutableQueue<Message>.Empty
                                );

                                return context with { Behaviour = Running(state) };
                            },
                            onFaulted: fault => {
                                start.ReplyChannel.Fault(fault);
                                return context;
                            }
                        );
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Unstarted)}' behaviour.");
            }
        };

    static Behaviour<Protocol> Running(State state) =>
        async context => {
            switch (context.Message) {
                case ConsumeMessage(var deliveryInfo, var properties, var buffer) when state.AvailableHandlers.Any(): {
                    var availableHandlers = state.AvailableHandlers.Dequeue(out var handler);
                    await handler.HandleMessage(state.Dispatcher, state.ConsumerConfiguration, deliveryInfo, properties, buffer);

                    return context with {
                        Behaviour = Running(state with {
                            AvailableHandlers = availableHandlers,
                            BusyHandlers      = state.BusyHandlers.Add(handler)
                        })
                    };
                }
                case ConsumeMessage(var deliveryInfo, var properties, var buffer) when state.AvailableHandlers.IsEmpty: {
                    return context with {
                        Behaviour = Running(state with {
                            Inbox = state.Inbox.Enqueue(new Message(deliveryInfo, properties, buffer))
                        })
                    };
                }
                case HandlerReady(var handler) when state.Inbox.IsEmpty: {
                    return context with {
                        Behaviour = Running(state with {
                            AvailableHandlers = state.AvailableHandlers.Enqueue(handler),
                            BusyHandlers      = state.BusyHandlers.Remove(handler)
                        })
                    };
                }
                case HandlerReady(var handler) when state.Inbox.Any(): {
                    var inbox = state.Inbox.Dequeue(out var message);

                    await handler.HandleMessage(state.Dispatcher, state.ConsumerConfiguration, message.DeliveryInfo, message.Properties, message.Body);

                    return context with {
                        Behaviour = Running(state with { Inbox = inbox })
                    };
                }
                case Stop: {
                    var processManager = RequestReplyAgent<BasicCancel, BasicCancelOk>.StartNew(
                        receivedFrames   : state.ReceivedFrames,
                        dispatcher       : state.Dispatcher,
                        cancellationToken: CancellationToken.None
                    );

                    await processManager.Request(new BasicCancel(state.ConsumerTag, NoWait: false))
                        .ContinueWith(
                            onCompleted: async _ => {
                                foreach (var handlerAgent in state.AvailableHandlers)
                                    await handlerAgent.Stop();

                                foreach (var handlerAgent in state.BusyHandlers)
                                    await handlerAgent.Stop();

                                await state.Assembler.Stop();

                                await context.Self.StopAsync();
                            },
                            onFaulted: fault => {
                                // TODO
                            }
                        );

                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Running)}' behaviour.");
            }
        };
}
