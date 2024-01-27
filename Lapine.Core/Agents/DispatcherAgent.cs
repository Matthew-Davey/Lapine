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

class DispatcherAgent : IDispatcherAgent {
    readonly IAgent<Protocol> _agent;

    DispatcherAgent(IAgent<Protocol> agent) =>
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));

    abstract record Protocol;
    record DispatchTo(ISocketAgent SocketAgent, UInt16 ChannelId) : Protocol;
    record Dispatch(Object Entity) : Protocol;

    static public IDispatcherAgent Create() =>
        new DispatcherAgent(Agent<Protocol>.StartNew(Ready));

    static async ValueTask<MessageContext<Protocol>> Ready(MessageContext<Protocol> context) {
        switch (context.Message) {
            case DispatchTo(var socketAgent, var channelId): {
                return context with { Behaviour = Dispatching(channelId, socketAgent) };
            }
            default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Ready)}' behaviour.");
        }
    }

    static Behaviour<Protocol> Dispatching(UInt16 channelId, ISocketAgent socketAgent) =>
        async context => {
            switch (context.Message) {
                case Dispatch { Entity: ProtocolHeader header }: {
                    await socketAgent.Transmit(header);
                    return context;
                }
                case Dispatch { Entity: RawFrame frame }: {
                    await socketAgent.Transmit(frame);
                    return context;
                }
                case Dispatch { Entity: ICommand command }: {
                    var frame = RawFrame.Wrap(in channelId, command);
                    await socketAgent.Transmit(frame);
                    return context;
                }
                case Dispatch { Entity: ContentHeader contentHeader }: {
                    var frame = RawFrame.Wrap(in channelId, contentHeader);
                    await socketAgent.Transmit(frame);
                    return context;
                }
                case Dispatch { Entity: ReadOnlyMemory<Byte> body }: {
                    var frame = RawFrame.Wrap(in channelId, body.Span);
                    await socketAgent.Transmit(frame);
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Dispatching)}' behaviour.");
            }
        };

    async Task IDispatcherAgent.DispatchTo(ISocketAgent socketAgent, UInt16 channelId) =>
        await _agent.PostAsync(new DispatchTo(socketAgent, channelId));

    async Task IDispatcherAgent.Dispatch(ProtocolHeader protocolHeader) =>
        await _agent.PostAsync(new Dispatch(protocolHeader));

    async Task IDispatcherAgent.Dispatch(RawFrame frame) =>
        await _agent.PostAsync(new Dispatch(frame));

    async Task IDispatcherAgent.Dispatch(ICommand command) =>
        await _agent.PostAsync(new Dispatch(command));

    async Task IDispatcherAgent.Dispatch(ContentHeader header) =>
        await _agent.PostAsync(new Dispatch(header));

    async Task IDispatcherAgent.Dispatch(ReadOnlyMemory<Byte> body) =>
        await _agent.PostAsync(new Dispatch(body));

    async Task IDispatcherAgent.Stop() =>
        await _agent.StopAsync();
}
