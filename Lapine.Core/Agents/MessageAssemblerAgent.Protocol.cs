namespace Lapine.Agents;

using Lapine.Protocol;

static partial class MessageAssemblerAgent {
    abstract record Protocol;
    record Begin(IObservable<RawFrame> Frames, AsyncReplyChannel ReplyChannel) : Protocol;
    record Stop : Protocol;
    record FrameReceived(Object Frame) : Protocol;
}
