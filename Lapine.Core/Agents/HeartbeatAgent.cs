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
            public record RemoteFlatline();

            internal record Beat();
        }

        static public Props Create() =>
            Props.FromProducer(() => new Actor());

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

                        var cancelHeartbeat = context.Scheduler().RequestRepeatedly(start.Frequency, start.Frequency, context.Self!, new Beat());
                        _behaviour.Become(Beating(start.Frequency, cancelHeartbeat, start.Dispatcher, start.Listener, UtcNow));
                        break;
                    };
                }
                return CompletedTask;
            }

            static Receive Beating(TimeSpan frequency, CancellationTokenSource cancelHeartbeat, PID dispatcher, PID listener, DateTime lastRemoteHeartbeat) =>
                (IContext context) => {
                    switch (context.Message) {
                        case Beat _: {
                            context.Send(dispatcher, Dispatch.Frame(RawFrame.Heartbeat));

                            if ((UtcNow - lastRemoteHeartbeat) > (frequency * 3)) {
                                context.Send(listener, new RemoteFlatline());
                            }

                            break;
                        }
                        case FrameReceived { Frame: { Type: FrameType.Heartbeat } }: {
                            lastRemoteHeartbeat = UtcNow;
                            break;
                        }
                        case Stopping _: {
                            cancelHeartbeat.Cancel();
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }
    }
}
