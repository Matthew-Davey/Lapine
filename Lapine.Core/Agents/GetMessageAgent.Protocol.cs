namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

static partial class GetMessageAgent {
    abstract record Protocol;
    record GetMessage(String Queue, Acknowledgements Acknowledgements, AsyncReplyChannel<GetMessageResult> ReplyChannel) : Protocol;
    record FrameReceived(Object Frame) : Protocol;
    record Timeout : Protocol;
}
