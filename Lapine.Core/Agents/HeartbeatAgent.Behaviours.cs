namespace Lapine.Agents;

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Lapine.Protocol;

using static System.DateTime;

static partial class HeartbeatAgent {
    readonly record struct State(
        IDisposable Subscription,
        TimeSpan HeartbeatFrequency,
        IDispatcherAgent Dispatcher,
        Subject<HeartbeatEvent> HeartbeatEvents,
        DateTime LastRemoteHeartbeat
    );

    static Behaviour<Protocol> Idle() =>
        async context => {
            switch (context.Message) {
                case StartHeartbeat(var receivedFrames, var dispatcher, var frequency, var replyChannel): {
                    if (frequency == TimeSpan.Zero)
                        return context;

                    var subscription = receivedFrames
                        .Where(frame => frame.Channel == 0)
                        .Where(frame => frame.Type == FrameType.Heartbeat)
                        .Subscribe(message => context.Self.PostAsync(new FrameReceived(message)));

                    var heartbeatEvents = new Subject<HeartbeatEvent>();

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

    static Behaviour<Protocol> Beating(State state) =>
        async context => {
            switch (context.Message) {
                case Beat: {
                    if ((UtcNow - state.LastRemoteHeartbeat) > (state.HeartbeatFrequency * 3)) {
                        state.HeartbeatEvents.OnNext(new RemoteFlatline());
                    }
                    await state.Dispatcher.Dispatch(RawFrame.Heartbeat);

                    var cts = new CancellationTokenSource(state.HeartbeatFrequency);
                    cts.Token.Register(() => context.Self.PostAsync(new Beat()));
                    return context;
                }
                case FrameReceived({ Type: FrameType.Heartbeat }): {
                    return context with {
                        Behaviour = Beating(state with {
                            LastRemoteHeartbeat = UtcNow
                        })
                    };
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Beating)}' behaviour.");
            }
        };
}
