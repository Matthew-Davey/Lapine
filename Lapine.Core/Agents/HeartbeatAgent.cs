namespace Lapine.Agents {
    using System;
    using System.Dynamic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lapine.Protocol;
    using Proto;
    using Proto.Timers;

    using static System.DateTimeOffset;
    using static System.Threading.Tasks.Task;

    class HeartbeatAgent : IActor {
        readonly PID _listener;
        readonly dynamic _state;

        public HeartbeatAgent(PID listener) {
            _listener = listener;
            _state = new ExpandoObject();
        }

        public Task ReceiveAsync(IContext context) {
            switch (context.Message) {
                case (":start-heartbeat-transmission", UInt16 frequency): {
                    _state.HeartbeatFrequency = frequency;
                    _state.MissedHeartbeats = 0;
                    _state.LastReceivedHeartbeat = DateTime.UtcNow;
                    _state.Scheduler = new Scheduler(context);
                    var cancellationTokenSource = _state.Scheduler.SendRepeatedly(
                        delay                  : TimeSpan.FromSeconds(frequency),
                        interval               : TimeSpan.FromSeconds(frequency),
                        target                 : context.Self,
                        message                : (":beat")
                    );
                    _state.SchedulerCancellationTokenSource = cancellationTokenSource;
                    break;
                }
                case (":beat"): {
                    context.Send(_listener, (":transmit", RawFrame.Heartbeat));

                    if ((DateTime.UtcNow - _state.LastReceivedHeartbeat).TotalSeconds > _state.HeartbeatFrequency)
                        _state.MissedHeartbeats++;

                    if (_state.MissedHeartbeats > 3)
                        context.Send(_listener, (":remote-flatline", _state.LastReceivedHeartbeat));

                    break;
                }
                case (":receive", RawFrame frame) when frame.Type == FrameType.Heartbeat: {
                    _state.MissedHeartbeats = 0;
                    _state.LastReceivedHeartbeat = UtcNow;
                    break;
                }
            }
            return CompletedTask;
        }
    }
}
