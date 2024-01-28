namespace Lapine.Agents;

using Lapine.Protocol;
using Lapine.Protocol.Commands;

static partial class DispatcherAgent {
    static Behaviour<Protocol> Ready() =>
        async context => {
            switch (context.Message) {
                case DispatchTo(var socketAgent, var channelId): {
                    return context with { Behaviour = Dispatching(channelId, socketAgent) };
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Ready)}' behaviour.");
            }
        };

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
}
