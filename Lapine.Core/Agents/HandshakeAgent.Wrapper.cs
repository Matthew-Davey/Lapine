namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

static partial class HandshakeAgent {
    class Wrapper(IAgent<Protocol> agent) : IHandshakeAgent {
        async Task<ConnectionAgreement> IHandshakeAgent.StartHandshake(ConnectionConfiguration connectionConfiguration) {
            return await agent.PostAndReplyAsync<ConnectionAgreement>(replyChannel => new StartHandshake(connectionConfiguration, replyChannel));
        }
    }
}
