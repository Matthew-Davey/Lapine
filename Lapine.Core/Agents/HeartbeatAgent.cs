namespace Lapine.Agents;

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Lapine.Protocol;

using static System.DateTime;
using static Lapine.Agents.DispatcherAgent.Protocol;
using static Lapine.Agents.HeartbeatAgent.Protocol;

static class HeartbeatAgent {
    static public class Protocol {
        public record StartHeartbeat(IObservable<RawFrame> ReceivedFrames, IAgent Dispatcher, TimeSpan Frequency);
        public record RemoteFlatline;
        internal record Beat;
    }

    static public IAgent Create() =>
        Agent.StartNew(Idle());

    readonly record struct State(
        IDisposable Subscription,
        TimeSpan HeartbeatFrequency,
        IAgent Dispatcher,
        Subject<Object> HeartbeatEvents,
        DateTime LastRemoteHeartbeat
    );

    static Behaviour Idle() =>
        async context => {
            switch (context.Message) {
                case Stopped: {
                    return context;
                }
                case (StartHeartbeat(var receivedFrames, var dispatcher, var frequency), AsyncReplyChannel replyChannel): {
                    if (frequency == TimeSpan.Zero)
                        return context;

                    var subscription = receivedFrames
                        .Where(frame => frame.Channel == 0)
                        .Where(frame => frame.Type == FrameType.Heartbeat)
                        .Subscribe(message => context.Self.PostAsync(message));

                    var heartbeatEvents = new Subject<Object>();

                    var cts = new CancellationTokenSource(frequency);
                    cts.Token.Register(() => context.Self.PostAsync(new Beat()));

                    replyChannel.Reply(heartbeatEvents);

                    return context with {
                        Behaviour = Beating(new State(
                            Subscription       : subscription,
                            HeartbeatFrequency : frequency,
                            Dispatcher         : dispatcher,
                            HeartbeatEvents    : heartbeatEvents,
                            LastRemoteHeartbeat: UtcNow
                        ))
                    };
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Idle)}' behaviour.");
            }
        };

    static Behaviour Beating(State state) =>
        async context => {
            switch (context.Message) {
                case Beat: {
                    if ((UtcNow - state.LastRemoteHeartbeat) > (state.HeartbeatFrequency * 3)) {
                        state.HeartbeatEvents.OnNext(new RemoteFlatline());
                    }
                    await state.Dispatcher.PostAsync(Dispatch.Frame(RawFrame.Heartbeat));

                    var cts = new CancellationTokenSource(state.HeartbeatFrequency);
                    cts.Token.Register(() => context.Self.PostAsync(new Beat()));
                    return context;
                }
                case RawFrame { Type: FrameType.Heartbeat }: {
                    return context with {
                        Behaviour = Beating(state with {
                            LastRemoteHeartbeat = UtcNow
                        })
                    };
                }
                case Stopped: {
                    state.Subscription.Dispose();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Beating)}' behaviour.");
            }
        };
}
