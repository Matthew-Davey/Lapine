namespace Lapine.Agents {
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Lapine.Protocol;
    using Proto;
    using Proto.Timers;

    using static System.DateTime;
    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.DispatcherAgent.Protocol;
    using static Lapine.Agents.HeartbeatAgent.Protocol;
    using static Lapine.Agents.SocketAgent.Protocol;

    static class HeartbeatAgent {
        static public class Protocol {
            public record StartHeartbeat(PID Dispatcher, TimeSpan Frequency, PID Listener);
            public record RemoteFlatline;

            internal record Beat;
        }

        static public Props Create() =>
            Props.FromProducer(() => new Actor());

        readonly record struct State(
            Guid SubscriptionId,
            TimeSpan HeartbeatFrequency,
            CancellationTokenSource ScheduledHeartbeat,
            PID Dispatcher,
            PID Listener,
            DateTime LastRemoteHeartbeat
        );

        class Actor : IActor {
            readonly Behavior _behaviour;

            public Actor() =>
                _behaviour = new Behavior(Idle);

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Idle(IContext context) {
                switch (context.Message) {
                    case StartHeartbeat start: {
                        if (start.Frequency == TimeSpan.Zero)
                            break;

                        var subscription = context.System.EventStream.Subscribe<FrameReceived>(
                            predicate: message => message is { Frame: { Type: FrameType.Heartbeat } },
                            action   : message => context.Send(context.Self!, message)
                        );
                        var scheduledHeartbeat = context.Scheduler().RequestRepeatedly(
                            delay   : start.Frequency,
                            interval: start.Frequency,
                            target  : context.Self!,
                            message : new Beat()
                        );
                        _behaviour.Become(Beating(new State(
                            SubscriptionId     : subscription.Id,
                            HeartbeatFrequency : start.Frequency,
                            ScheduledHeartbeat : scheduledHeartbeat,
                            Dispatcher         : start.Dispatcher,
                            Listener           : start.Listener,
                            LastRemoteHeartbeat: UtcNow
                        )));
                        break;
                    };
                }
                return CompletedTask;
            }

            Receive Beating(State state) =>
                (IContext context) => {
                    switch (context.Message) {
                        case Beat _: {
                            if ((UtcNow - state.LastRemoteHeartbeat) > (state.HeartbeatFrequency * 3)) {
                                context.Send(state.Listener, new RemoteFlatline());
                            }
                            context.Send(state.Dispatcher, Dispatch.Frame(RawFrame.Heartbeat));
                            break;
                        }
                        case FrameReceived { Frame: { Type: FrameType.Heartbeat } }: {
                            _behaviour.Become(Beating(state with {
                                LastRemoteHeartbeat = UtcNow
                            }));
                            break;
                        }
                        case Stopping _: {
                            state.ScheduledHeartbeat.Cancel();
                            context.System.EventStream.Unsubscribe(state.SubscriptionId);
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }
    }
}
