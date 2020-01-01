namespace Lapine.Agents {
    using System;
    using System.Dynamic;
    using System.Threading.Tasks;
    using Lapine.Agents.Middleware;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;

    using static Lapine.Agents.Messages;
    using static Proto.Actor;

    public class ChannelAgent : IActor {
        readonly Behavior _behaviour;
        readonly dynamic _state;

        public ChannelAgent() {
            _behaviour = new Behavior(Unstarted);
            _state     = new ExpandoObject();
        }

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
                case (Outbound, _): {
                    context.Forward(context.Parent);
                    return Done;
                }
                case (Inbound, RawFrame frame) when frame.Type == FrameType.Heartbeat: {
                    context.Forward(_state.HeartbeatAgent);
                    return Done;
                }
                case ConnectionConfiguration connectionConfiguration: {
                    _state.ConnectionConfiguration = connectionConfiguration;
                    return Done;
                }
                case (Inbound, ConnectionStart message): {
                    _state.HandshakeAgent = context.SpawnNamed(
                        name: "handshake",
                        props: Props.FromProducer(() => new HandshakeAgent(_state.ConnectionConfiguration))
                            .WithContextDecorator(context => new LoggingContextDecorator(context))
                    );
                    context.Forward(_state.HandshakeAgent);
                    _behaviour.BecomeStacked(AwaitHandshake);
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
                case (Outbound, _): {
                    context.Forward(context.Parent);
                    return Done;
                }
                case (Inbound, ICommand _): {
                    context.Forward(_state.HandshakeAgent);
                    return Done;
                }
                case (StartHeartbeatTransmission, UInt16 frequency): {
                    _state.HeartbeatAgent = context.SpawnNamed(
                        name: "heartbeat",
                        props: Props.FromProducer(() => new HeartbeatAgent())
                            .WithContextDecorator(LoggingContextDecorator.Create)
                    );
                    context.Forward(_state.HeartbeatAgent);
                    return Done;
                }
                case (HandshakeCompleted): {
                    _behaviour.UnbecomeStacked();
                    context.Forward(context.Parent);
                    return Done;
                }
                case (AuthenticationFailed): {
                    context.Self.Stop(); // TODO: fail gracefully
                    return Done;
                }
                // TODO: case: handshake timed out..
                default: return Done;
            }
        }
    }
}
