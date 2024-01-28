namespace Lapine.Agents;

using Lapine.Client;

static partial class HandshakeAgent {
    class Wrapper(IAgent<Protocol> agent) : IHandshakeAgent {
        async Task<HandshakeResult> IHandshakeAgent.StartHandshake(ConnectionConfiguration connectionConfiguration) {
            var reply = await agent.PostAndReplyAsync(replyChannel => new StartHandshake(connectionConfiguration, replyChannel));
            return (HandshakeResult) reply;
        }
    }
}
