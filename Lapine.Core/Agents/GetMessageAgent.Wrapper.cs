namespace Lapine.Agents;

using Lapine.Client;

static partial class GetMessageAgent {
    class Wrapper(IAgent<Protocol> agent) : IGetMessageAgent {
        async Task<GetMessageResult> IGetMessageAgent.GetMessages(String queue, Acknowledgements acknowledgements) {
            return await agent.PostAndReplyAsync<GetMessageResult>(replyChannel => new GetMessage(queue, acknowledgements, replyChannel));
        }
    }
}
