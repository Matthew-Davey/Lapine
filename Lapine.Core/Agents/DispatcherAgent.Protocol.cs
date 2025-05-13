namespace Lapine.Agents;

static partial class DispatcherAgent {
    abstract record Protocol;
    record DispatchTo(ISocketAgent SocketAgent, UInt16 ChannelId) : Protocol;
    record Dispatch(Object Entity) : Protocol;
}
