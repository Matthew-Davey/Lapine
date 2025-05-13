namespace Lapine.Agents;

using Lapine.Protocol;
using Lapine.Protocol.Commands;

interface IDispatcherAgent {
    Task DispatchTo(ISocketAgent socketAgent, UInt16 channelId);
    Task Dispatch(ProtocolHeader protocolHeader);
    Task Dispatch(RawFrame frame);
    Task Dispatch(ICommand command);
    Task Dispatch(ContentHeader header);
    Task Dispatch(ReadOnlyMemory<Byte> body);
    Task Stop();
}

static partial class DispatcherAgent {
    static public IDispatcherAgent Create() =>
        new Wrapper(Agent<Protocol>.StartNew(Ready()));
}
