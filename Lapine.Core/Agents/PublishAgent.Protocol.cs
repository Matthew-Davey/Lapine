namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

static partial class PublishAgent {
    abstract record Protocol;

    record PublishMessage(
        String Exchange,
        String RoutingKey,
        RoutingFlags RoutingFlags,
        (BasicProperties Properties, ReadOnlyMemory<Byte> Body) Message,
        AsyncReplyChannel ReplyChannel
    ) : Protocol;

    record Timeout : Protocol;

    record FrameReceived(ICommand Command) : Protocol;
}
