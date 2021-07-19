namespace Lapine.Agents {
    using System;
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using Proto;

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.FrameRouterAgent.Protocol;
    using static Lapine.Agents.SocketAgent.Protocol;

    static class FrameRouterAgent {
        static public class Protocol {
            public record AddRoutee(UInt16 ChannelId, PID Routee);
            public record RemoveRoutee(UInt16 ChannelId, PID Routee);
            public record Reset();
        }

        static public Props Create() =>
            Props.FromProducer(() => new Actor());

        class Actor : IActor {
            readonly Behavior _behaviour;

            public Actor() =>
                _behaviour = new Behavior(Routing(ImmutableDictionary<UInt16, IImmutableSet<PID>>.Empty));

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            public Receive Routing(IImmutableDictionary<UInt16, IImmutableSet<PID>> routees) =>
                (IContext context) => {
                    switch (context.Message) {
                        case AddRoutee add: {
                            var newRoutees = routees.ContainsKey(add.ChannelId) switch {
                                true  => routees.SetItem(add.ChannelId, routees[add.ChannelId].Add(add.Routee)),
                                false => routees.Add(add.ChannelId, ImmutableHashSet<PID>.Empty.Add(add.Routee))
                            };
                            _behaviour.Become(Routing(newRoutees));
                            break;
                        }
                        case RemoveRoutee remove: {
                            if (routees.ContainsKey(remove.ChannelId)) {
                                _behaviour.Become(Routing(routees.SetItem(remove.ChannelId, routees[remove.ChannelId].Remove(remove.Routee))));
                            }
                            break;
                        }
                        case Reset _: {
                            _behaviour.Become(Routing(ImmutableDictionary<UInt16, IImmutableSet<PID>>.Empty));
                            break;
                        }
                        case FrameReceived received: {
                            if (routees.ContainsKey(received.Frame.Channel)) {
                                foreach (var routee in routees[received.Frame.Channel]) {
                                    context.Forward(routee);
                                }
                            }
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }
    }
}
