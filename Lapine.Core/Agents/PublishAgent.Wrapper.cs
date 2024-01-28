namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

static partial class PublishAgent {
    class Wrapper(IAgent<Protocol> agent) : IPublishAgent {
        async Task<Result<Boolean>> IPublishAgent.Publish(String exchange, String routingKey, RoutingFlags routingFlags, (BasicProperties Properties, ReadOnlyMemory<Byte> Body) message) {
            var reply = await agent.PostAndReplyAsync(replyChannel => new PublishMessage(exchange, routingKey, routingFlags, message, replyChannel));
            return (Result<Boolean>) reply;
        }
    }
}
