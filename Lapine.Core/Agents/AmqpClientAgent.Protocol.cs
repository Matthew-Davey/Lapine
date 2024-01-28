namespace Lapine.Agents;

using Lapine.Client;

static partial class AmqpClientAgent {
    abstract record Protocol;

    record EstablishConnection(
        ConnectionConfiguration Configuration,
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;

    record OpenChannel(
        AsyncReplyChannel ReplyChannel,
        CancellationToken CancellationToken = default
    ) : Protocol;

    record Disconnect(
        AsyncReplyChannel ReplyChannel
    ) : Protocol;

    record HeartbeatEventEventReceived(
        Object Message
    ) : Protocol;
}
