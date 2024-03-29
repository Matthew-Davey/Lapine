namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

static partial class HandshakeAgent {
    abstract record Protocol;
    record StartHandshake(ConnectionConfiguration ConnectionConfiguration, AsyncReplyChannel<ConnectionAgreement> ReplyChannel) : Protocol;
    record FrameReceived(ICommand Frame) : Protocol;
    record Timeout(TimeoutException Exception) : Protocol;
    record ConnectionEventReceived(Object Message) : Protocol;
}
