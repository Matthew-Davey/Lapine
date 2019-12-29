namespace Lapine.Agents {
    using System;
    using System.Threading.Tasks;
    using Lapine.Agents.Commands;
    using Lapine.Agents.Events;
    using Lapine.Agents.Middleware;
    using Lapine.Protocol.Commands;
    using Proto;

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
                case TransmitCommand message: {
                    context.Send(context.Parent, message.Command);
                    return Done;
                }
                case ConnectionStart message: {
                    _handshakeAgent = context.SpawnNamed(
                        Props.FromProducer(() => new HandshakeAgent("/"))
                            .WithContextDecorator(context => new LoggingContextDecorator(context)),
                        "handshake"
                    );
                    context.Forward(_handshakeAgent);
                    _behaviour.Become(AwaitHandshake);
                    return Done;
                }
                case ConnectionClose message: {
                    context.Send(context.Parent, new ConnectionCloseOk());
                    context.Self.Stop();
                    return Done;
                }
            }
            return Done;
        }

        Task AwaitHandshake(IContext context) {
            switch (context.Message) {
                case ConnectionStartOk _: {
                    context.Forward(context.Parent);
                    return Done;
                }
                case ConnectionSecure _: {
                    context.Forward(_handshakeAgent);
                    return Done;
                }
                case ConnectionSecureOk _: {
                    context.Forward(context.Parent);
                    return Done;
                }
                case ConnectionTune _: {
                    context.Forward(_handshakeAgent);
                    return Done;
                }
                case ConnectionTuneOk _: {
                    context.Forward(context.Parent);
                    return Done;
                }
                case ConnectionOpen _: {
                    context.Forward(context.Parent);
                    return Done;
                }
                case ConnectionOpenOk _: {
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
                default: return Done;
            }
        }
    }
}
