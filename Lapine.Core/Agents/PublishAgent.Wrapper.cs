namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

static partial class PublishAgent {
    class Wrapper(IAgent<Protocol> agent) : IPublishAgent {
        async Task IPublishAgent.Publish(String exchange, String routingKey, RoutingFlags routingFlags, (BasicProperties Properties, ReadOnlyMemory<Byte> Body) message) {
            await agent.PostAndReplyAsync(replyChannel => new PublishMessage(exchange, routingKey, routingFlags, message, replyChannel));
        }
    }
}
