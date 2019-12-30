namespace Lapine.Agents {
    using System;
    using System.Threading.Tasks;
    using Lapine.Agents.Events;
    using Lapine.Agents.Middleware;
    using Lapine.Protocol.Commands;
    using Proto;

    using static Lapine.Direction;
    using static Proto.Actor;

    public class ChannelAgent : IActor {
        readonly Behavior _behaviour;
        PID _handshakeAgent;

        public ChannelAgent() =>
            _behaviour = new Behavior(Unstarted);

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Unstarted(IContext context) {
            switch (context.Message) {
                case Started _: {
                    _behaviour.Become(Running);
                    break;
                }
            }
            return Done;
        }

        Task Running(IContext context) {
            switch (context.Message) {
                case (Outbound, ICommand message): {
                    context.Send(context.Parent, message);
                    return Done;
                }
                case (Inbound, ConnectionStart message): {
                    _handshakeAgent = context.SpawnNamed(
                        name: "handshake",
                        props: Props.FromProducer(() => new HandshakeAgent("/"))
                            .WithContextDecorator(context => new LoggingContextDecorator(context))
                    );
                    context.Forward(_handshakeAgent);
                    _behaviour.Become(AwaitHandshake);
                    return Done;
                }
                case (Inbound, ConnectionClose message): {
                    context.Send(context.Parent, (Outbound, new ConnectionCloseOk()));
                    context.Self.Stop();
                    return Done;
                }
            }
            return Done;
        }

        Task AwaitHandshake(IContext context) {
            switch (context.Message) {
                case (Outbound, ICommand command): {
                    context.Forward(context.Parent);
                    return Done;
                }
                case (Inbound, ICommand command): {
                    context.Forward(_handshakeAgent);
                    return Done;
                }
                case HandshakeCompleted _: {
                    _behaviour.Become(Running);
                    return Done;
                }
                case AuthenticationFailed _: {
                    context.Self.Stop(); // TODO: fail gracefully
                    return Done;
                }
                // TODO: case: handshake timed out..
                default: return Done;
            }
        }
    }
}
