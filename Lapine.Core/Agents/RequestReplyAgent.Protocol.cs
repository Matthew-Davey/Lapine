namespace Lapine.Agents;

using Lapine.Protocol.Commands;

static partial class RequestReplyAgent<TRequest, TReply> where TRequest : ICommand where TReply : ICommand {
    abstract record Protocol;
    record SendRequest(TRequest Request, AsyncReplyChannel<TReply> ReplyChannel) : Protocol;
    record OnFrameReceived(ICommand Command) : Protocol;
    record OnTimeout : Protocol;
}
