namespace Lapine.Agents;

using Lapine.Protocol;

static partial class HeartbeatAgent {
    abstract record Protocol;

    record StartHeartbeat(
        IObservable<RawFrame> ReceivedFrames,
        IDispatcherAgent Dispatcher,
        TimeSpan Frequency,
        AsyncReplyChannel ReplyChannel
    ) : Protocol;

    record Beat : Protocol;

    record FrameReceived(RawFrame Frame) : Protocol;
}
