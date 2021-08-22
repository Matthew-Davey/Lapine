namespace Lapine.Agents;

using System;
using System.Threading.Tasks;
using Lapine.Protocol;
using Lapine.Protocol.Commands;
using Proto;

using static System.Threading.Tasks.Task;
using static Lapine.Agents.DispatcherAgent.Protocol;
using static Lapine.Agents.SocketAgent.Protocol;

static class DispatcherAgent {
    static public class Protocol {
        public record DispatchTo(PID TxD, UInt16 ChannelId);
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

    static public Props Create() =>
        Props.FromProducer(() => new Actor());

    class Actor : IActor {
        readonly Behavior _behaviour;

        public Actor() =>
            _behaviour = new Behavior(Ready);

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Ready(IContext context) {
            switch (context.Message) {
                case DispatchTo dispatch: {
                    _behaviour.Become(Dispatching(dispatch.ChannelId, dispatch.TxD));
                    break;
                }
            }
            return CompletedTask;
        }

        static Receive Dispatching(UInt16 channelId, PID txd) =>
            (IContext context) => {
                switch (context.Message) {
                    case Dispatch { Entity: ProtocolHeader header }: {
                        context.Send(txd, new Transmit(header));
                        break;
                    }
                    case Dispatch { Entity: RawFrame frame }: {
                        context.Send(txd, new Transmit(frame));
                        break;
                    }
                    case Dispatch { Entity: ICommand command }: {
                        var frame = RawFrame.Wrap(in channelId, command);
                        context.Send(txd, new Transmit(frame));
                        break;
                    }
                    case Dispatch { Entity: ContentHeader contentHeader }: {
                        var frame = RawFrame.Wrap(in channelId, contentHeader);
                        context.Send(txd, new Transmit(frame));
                        break;
                    }
                    case Dispatch { Entity: ReadOnlyMemory<Byte> body }: {
                        var frame = RawFrame.Wrap(in channelId, body.Span);
                        context.Send(txd, new Transmit(frame));
                        break;
                    }
                }

                return CompletedTask;
            };
    }
}
