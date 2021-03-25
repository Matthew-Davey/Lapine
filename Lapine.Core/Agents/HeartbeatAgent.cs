namespace Lapine.Agents {
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Lapine.Agents.Middleware;
    using Lapine.Protocol;
    using Proto;
    using Proto.Timers;

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.HeartbeatAgent.Protocol;

    static class HeartbeatAgent {
        static public class Protocol {
            public record StartHeartbeat(PID Dispatcher, TimeSpan Frequency, PID Listener);
            public record StopHeartbeat();
            public record RemoteFlatline();

            internal record Beat();
        }

        static public Props Create() =>
            Props.FromProducer(() => new Actor())
                .WithContextDecorator(LoggingContextDecorator.Create);

        class Actor : IActor {
            readonly Behavior _behaviour;

            public Actor() =>
                _behaviour = new Behavior(Idle);

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Idle(IContext context) {
                switch (context.Message) {
                    case StartHeartbeat start: {
                        var cancelHeartbeat = context.Scheduler().RequestRepeatedly(start.Frequency, start.Frequency, context.Self!, new Beat());
                        _behaviour.Become(Beating(start.Frequency, cancelHeartbeat, start.Dispatcher, start.Listener, DateTime.UtcNow));
                        break;
                    };
                }
                return CompletedTask;
            }

            Receive Beating(TimeSpan frequency, CancellationTokenSource cancelHeartbeat, PID dispatcher, PID listener, DateTime lastRemoteHeartbeat) =>
                (IContext context) => {
                    switch (context.Message) {
                        case Beat _: {
                            context.Send(dispatcher, RawFrame.Heartbeat);

                            if ((DateTime.UtcNow - lastRemoteHeartbeat) > (frequency * 3)) {
                                context.Send(listener, new RemoteFlatline());
                            }

                            break;
                        }
                        case StopHeartbeat _: {
                            cancelHeartbeat.Cancel();
                            _behaviour.Become(Idle);
                            break;
                        }
                        case SocketAgent.Protocol.FrameReceived received when received.Frame.Type == FrameType.Heartbeat: {
                            _behaviour.Become(Beating(frequency, cancelHeartbeat, dispatcher, listener, DateTime.UtcNow));
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }
    }
}
