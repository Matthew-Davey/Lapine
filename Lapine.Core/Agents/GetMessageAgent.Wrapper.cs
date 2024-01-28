namespace Lapine.Agents;

using Lapine.Client;

static partial class GetMessageAgent {
    class Wrapper(IAgent<Protocol> agent) : IGetMessageAgent {
        async Task<GetMessageResult> IGetMessageAgent.GetMessages(String queue, Acknowledgements acknowledgements) {
            switch (await agent.PostAndReplyAsync(replyChannel => new GetMessage(queue, acknowledgements, replyChannel))) {
                case GetMessageResult reply:
                    return reply;
                case Exception fault:
                    throw fault;
                default:
                    throw new Exception("Unexpected return value");
            }
        }
    }
}
