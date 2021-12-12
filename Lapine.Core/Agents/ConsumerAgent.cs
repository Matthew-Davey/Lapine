namespace Lapine.Agents;

using System.Collections.Immutable;
using Lapine.Agents.Middleware;
using Lapine.Agents.ProcessManagers;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;
using Proto;

using static Lapine.Agents.ConsumerAgent.Protocol;
using static Lapine.Agents.MessageHandlerAgent.Protocol;

static class ConsumerAgent {
    static public class Protocol {
        public record StartConsuming(
            UInt16 ChannelId,
            String ConsumerTag,
            PID Dispatcher,
            String Queue,
            ConsumerConfiguration ConsumerConfiguration,
            IReadOnlyDictionary<String, Object>? Arguments,
            TaskCompletionSource Promise
        );
        public record ConsumeMessage(
            DeliveryInfo Delivery,
            BasicProperties Properties,
            MemoryBufferWriter<Byte> Buffer
        );
    }

    static public Props Create() =>
        Props.FromProducer(() => new Actor())
            .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames());

    readonly record struct Message(
        DeliveryInfo DeliveryInfo,
        BasicProperties Properties,
        MemoryBufferWriter<Byte> Body
    );

    readonly record struct State(
        UInt16 ChannelId,
        String ConsumerTag,
        ConsumerConfiguration ConsumerConfiguration,
        PID Dispatcher,
        IImmutableQueue<Message> Inbox,
        IImmutableQueue<PID> AvailableHandlers,
        ImmutableList<PID> BusyHandlers
    );

    class Actor : IActor {
        readonly Behavior _behaviour;

        public Actor() =>
            _behaviour = new Behavior(Unstarted);

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        async Task Unstarted(IContext context) {
            switch (context.Message) {
                case StartConsuming start: {
                    var handlers = Enumerable.Range(0, start.ConsumerConfiguration.MaxDegreeOfParallelism)
                        .Select(i => context.SpawnNamed(
                            name: $"handler_{i}",
                            props: MessageHandlerAgent.Create()
                        ));
                    var promise = new TaskCompletionSource();
                    context.SpawnNamed(
                        name: "assembler",
                        props: MessageAssemblerAgent.Create(start.ChannelId, context.Self!)
                    );
                    context.Spawn(
                        RequestReplyProcessManager<BasicConsume, BasicConsumeOk>.Create(
                            channelId : start.ChannelId,
                            dispatcher: start.Dispatcher,
                            request   : new BasicConsume(
                                QueueName  : start.Queue,
                                ConsumerTag: start.ConsumerTag,
                                NoLocal    : false,
                                NoAck      : start.ConsumerConfiguration.Acknowledgements switch {
                                    Acknowledgements.Auto => true,
                                    _                     => false
                                },
                                Exclusive  : start.ConsumerConfiguration.Exclusive,
                                NoWait     : false,
                                Arguments  : start.Arguments ?? ImmutableDictionary<String, Object>.Empty
                            ),
                            timeout   : TimeSpan.FromMilliseconds(-1),
                            promise   : promise
                        )
                    );
                    await promise.Task.ContinueWith(
                        onCompleted: () => {
                            _behaviour.Become(Running(new State(
                                ChannelId            : start.ChannelId,
                                ConsumerTag          : start.ConsumerTag,
                                ConsumerConfiguration: start.ConsumerConfiguration,
                                Dispatcher           : start.Dispatcher,
                                Inbox                : ImmutableQueue<Message>.Empty,
                                AvailableHandlers    : handlers.ToImmutableQueue(),
                                BusyHandlers         : ImmutableList<PID>.Empty
                            )));
                            start.Promise.SetResult();
                        },
                        onFaulted: start.Promise.SetException
                    );
                    break;
                }
            }
        }

        Receive Running(State state) =>
            async (IContext context) => {
                switch (context.Message) {
                    case ConsumeMessage consume when state.AvailableHandlers.Any(): {
                        _behaviour.Become(Running(state with {
                            AvailableHandlers = state.AvailableHandlers.Dequeue(out var handler),
                            BusyHandlers      = state.BusyHandlers.Add(handler)
                        }));
                        context.Send(handler, new HandleMessage(
                            Dispatcher           : state.Dispatcher,
                            ConsumerConfiguration: state.ConsumerConfiguration,
                            Delivery             : consume.Delivery,
                            Properties           : consume.Properties,
                            Buffer               : consume.Buffer
                        ));
                        break;
                    }
                    case ConsumeMessage consume when state.AvailableHandlers.IsEmpty: {
                        _behaviour.Become(Running(state with {
                            Inbox = state.Inbox.Enqueue(new Message(
                                DeliveryInfo: consume.Delivery,
                                Properties  : consume.Properties,
                                Body        : consume.Buffer
                            ))
                        }));
                        break;
                    }
                    case HandlerReady ready when state.Inbox.IsEmpty: {
                        _behaviour.Become(Running(state with {
                            AvailableHandlers = state.AvailableHandlers.Enqueue(ready.Handler),
                            BusyHandlers = state.BusyHandlers.Remove(ready.Handler)
                        }));
                        break;
                    }
                    case HandlerReady ready when state.Inbox.Any(): {
                        _behaviour.Become(Running(state with {
                            Inbox = state.Inbox.Dequeue(out var message)
                        }));
                        context.Send(ready.Handler, new HandleMessage(
                            Dispatcher           : state.Dispatcher,
                            ConsumerConfiguration: state.ConsumerConfiguration,
                            Delivery             : message.DeliveryInfo,
                            Properties           : message.Properties,
                            Buffer               : message.Body
                        ));
                        break;
                    }
                    case Stopping _: {
                        var promise = new TaskCompletionSource();
                        context.Spawn(
                            RequestReplyProcessManager<BasicCancel, BasicCancelOk>.Create(
                                channelId : state.ChannelId,
                                dispatcher: state.Dispatcher,
                                request   : new BasicCancel(state.ConsumerTag, false),
                                timeout   : TimeSpan.FromMilliseconds(-1),
                                promise   : promise
                            )
                        );
                        await promise.Task;
                        break;
                    }
                }
            };
    }
}
