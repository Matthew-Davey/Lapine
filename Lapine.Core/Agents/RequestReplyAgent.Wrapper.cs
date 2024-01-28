namespace Lapine.Agents;

using Lapine.Protocol.Commands;

static partial class RequestReplyAgent<TRequest, TReply> where TRequest : ICommand where TReply : ICommand {
    class Wrapper(IAgent<Protocol> agent) : IRequestReplyAgent<TRequest, TReply> {
        async Task<Result<TReply>> IRequestReplyAgent<TRequest, TReply>.Request(TRequest request) {
            var reply = (Result<TReply>) await agent.PostAndReplyAsync(replyChannel => new SendRequest(request, replyChannel));
            return reply;
        }
    }
}
