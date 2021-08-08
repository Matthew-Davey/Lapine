namespace Lapine.Agents {
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using Lapine.Client;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.ConsumerAgent.Protocol;
    using static Lapine.Agents.DispatcherAgent.Protocol;
    using static Lapine.Agents.MessageHandlerAgent.Protocol;

    static class ConsumerAgent {
        static public class Protocol {
            public record StartConsuming(
                String ConsumerTag,
                PID Dispatcher,
                ConsumerConfiguration ConsumerConfiguration
            );
            public record ConsumeMessage(
                DeliveryInfo Delivery,
                BasicProperties Properties,
                MemoryBufferWriter<Byte> Buffer
            );
        }

        static public Props Create() =>
            Props.FromProducer(() => new Actor());

        readonly record struct Message(
            DeliveryInfo DeliveryInfo,
            BasicProperties Properties,
            MemoryBufferWriter<Byte> Body
        );

        readonly record struct State(
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

            Task Unstarted(IContext context) {
                switch (context.Message) {
                    case StartConsuming start: {
                        var handlers = Enumerable.Range(0, start.ConsumerConfiguration.MaxDegreeOfParallelism)
                            .Select(i => context.SpawnNamed(
                                name: $"handler_{i}",
                                props: MessageHandlerAgent.Create()
                            ));
                        _behaviour.Become(Running(new State(
                            ConsumerTag          : start.ConsumerTag,
                            ConsumerConfiguration: start.ConsumerConfiguration,
                            Dispatcher           : start.Dispatcher,
                            Inbox                : ImmutableQueue<Message>.Empty,
                            AvailableHandlers    : handlers.ToImmutableQueue(),
                            BusyHandlers         : ImmutableList<PID>.Empty
                        )));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive Running(State state) =>
                (IContext context) => {
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
                            context.Send(state.Dispatcher, Dispatch.Command(new BasicCancel(state.ConsumerTag, false)));
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }
    }
}
