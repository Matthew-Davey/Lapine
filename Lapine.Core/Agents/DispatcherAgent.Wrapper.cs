namespace Lapine.Agents;

using Lapine.Protocol;
using Lapine.Protocol.Commands;

static partial class DispatcherAgent {
    class Wrapper(IAgent<Protocol> agent) : IDispatcherAgent {
        async Task IDispatcherAgent.DispatchTo(ISocketAgent socketAgent, UInt16 channelId) =>
            await agent.PostAsync(new DispatchTo(socketAgent, channelId));

        async Task IDispatcherAgent.Dispatch(ProtocolHeader protocolHeader) =>
            await agent.PostAsync(new Dispatch(protocolHeader));

        async Task IDispatcherAgent.Dispatch(RawFrame frame) =>
            await agent.PostAsync(new Dispatch(frame));

        async Task IDispatcherAgent.Dispatch(ICommand command) =>
            await agent.PostAsync(new Dispatch(command));

        async Task IDispatcherAgent.Dispatch(ContentHeader header) =>
            await agent.PostAsync(new Dispatch(header));

        async Task IDispatcherAgent.Dispatch(ReadOnlyMemory<Byte> body) =>
            await agent.PostAsync(new Dispatch(body));

        async Task IDispatcherAgent.Stop() =>
            await agent.StopAsync();
    }
}
