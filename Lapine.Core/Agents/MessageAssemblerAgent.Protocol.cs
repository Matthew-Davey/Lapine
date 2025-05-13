namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

static partial class MessageAssemblerAgent {
    abstract record Protocol;
    record Begin(IObservable<RawFrame> Frames, AsyncReplyChannel<IObservable<(DeliveryInfo DeliveryInfo, BasicProperties Properties, MemoryBufferWriter<Byte> Buffer)>> ReplyChannel) : Protocol;
    record Stop : Protocol;
    record FrameReceived(Object Frame) : Protocol;
}
