namespace Lapine.Agents;

using Lapine.Protocol;
using Lapine.Protocol.Commands;

using static Lapine.Agents.DispatcherAgent.Protocol;
using static Lapine.Agents.SocketAgent.Protocol;

static class DispatcherAgent {
    static public class Protocol {
        public record DispatchTo(IAgent SocketAgent, UInt16 ChannelId);
        public record Dispatch(Object Entity) {
            static public Dispatch ProtocolHeader(ProtocolHeader protocolHeader) =>
                new (protocolHeader);

            static public Dispatch Frame(RawFrame frame) =>
                new (frame);

            static public Dispatch Command(ICommand command) =>
                new (command);

            static public Dispatch ContentHeader(ContentHeader header) =>
                new (header);

            static public Dispatch ContentBody(ReadOnlyMemory<Byte> body) =>
                new (body);
        }
    }

    static public IAgent Create() =>
        Agent.StartNew(Ready);

    static async ValueTask<MessageContext> Ready(MessageContext context) {
        switch (context.Message) {
            case DispatchTo(var socketAgent, var channelId): {
                return context with { Behaviour = Dispatching(channelId, socketAgent) };
            }
            default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Ready)}' behaviour.");
        }
    }

    static Behaviour Dispatching(UInt16 channelId, IAgent socketAgent) =>
        async context => {
            switch (context.Message) {
                case Dispatch { Entity: ProtocolHeader header }: {
                    await socketAgent.PostAsync(new Transmit(header));
                    return context;
                }
                case Dispatch { Entity: RawFrame frame }: {
                    await socketAgent.PostAsync(new Transmit(frame));
                    return context;
                }
                case Dispatch { Entity: ICommand command }: {
                    var frame = RawFrame.Wrap(in channelId, command);
                    await socketAgent.PostAsync(new Transmit(frame));
                    return context;
                }
                case Dispatch { Entity: ContentHeader contentHeader }: {
                    var frame = RawFrame.Wrap(in channelId, contentHeader);
                    await socketAgent.PostAsync(new Transmit(frame));
                    return context;
                }
                case Dispatch { Entity: ReadOnlyMemory<Byte> body }: {
                    var frame = RawFrame.Wrap(in channelId, body.Span);
                    await socketAgent.PostAsync(new Transmit(frame));
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Dispatching)}' behaviour.");
            }
        };
}
