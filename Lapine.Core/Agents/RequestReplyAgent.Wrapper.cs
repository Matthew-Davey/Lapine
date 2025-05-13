namespace Lapine.Agents;

using Lapine.Protocol.Commands;

static partial class RequestReplyAgent<TRequest, TReply> where TRequest : ICommand where TReply : ICommand {
    class Wrapper(IAgent<Protocol> agent) : IRequestReplyAgent<TRequest, TReply> {
        async Task<TReply> IRequestReplyAgent<TRequest, TReply>.Request(TRequest request) {
            return await agent.PostAndReplyAsync<TReply>(replyChannel => new SendRequest(request, replyChannel));
        }
    }
}
