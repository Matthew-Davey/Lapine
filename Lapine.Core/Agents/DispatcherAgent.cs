namespace Lapine.Agents {
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
                        case ProtocolHeader header: {
                            context.Send(txd, new Transmit(header));
                            break;
                        }
                        case RawFrame frame: {
                            context.Send(txd, new Transmit(frame));
                            break;
                        }
                        case ICommand command: {
                            var frame = RawFrame.Wrap(in channelId, command);
                            context.Send(txd, new Transmit(frame));
                            break;
                        }
                        case ContentHeader contentHeader: {
                            var frame = RawFrame.Wrap(in channelId, contentHeader);
                            context.Send(txd, new Transmit(frame));
                            break;
                        }
                        case ReadOnlyMemory<Byte> body: {
                            var frame = RawFrame.Wrap(in channelId, body.Span);
                            context.Send(txd, new Transmit(frame));
                            break;
                        }
                    }

                    return CompletedTask;
                };
        }
    }
}
