namespace Lapine.Agents {
    using System;
    using System.Dynamic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lapine.Protocol;
    using Proto;
    using Proto.Schedulers.SimpleScheduler;

    using static System.DateTimeOffset;
    using static Lapine.Agents.Messages;
    using static Lapine.Direction;
    using static Proto.Actor;

    public class HeartbeatAgent : IActor {
        readonly dynamic _state;

        public HeartbeatAgent() =>
            _state = new ExpandoObject();

        public Task ReceiveAsync(IContext context) {
            switch (context.Message) {
                case (StartHeartbeatTransmission, UInt16 frequency): {
                    _state.HeartbeatFrequency = frequency;
                    _state.Scheduler = new SimpleScheduler(context);
                    _state.Scheduler.ScheduleTellRepeatedly(
                        delay                  : TimeSpan.FromSeconds(frequency),
                        interval               : TimeSpan.FromSeconds(frequency),
                        target                 : context.Parent,
                        message                : (Outbound, RawFrame.Heartbeat),
                        cancellationTokenSource: out CancellationTokenSource cancellationTokenSource
                    );
                    _state.SchedulerCancellationTokenSource = cancellationTokenSource;
                    break;
                }
                case (Inbound, RawFrame frame) when frame.Type == FrameType.Heartbeat: {
                    _state.LastReceivedHeartbeat = UtcNow;
                    break;
                }
            }
            return Done;
        }
    }
}
