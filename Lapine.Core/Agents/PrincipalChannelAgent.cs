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

    public class PrincipalChannelAgent : IActor {
        readonly Behavior _behaviour;
        readonly ConnectionConfiguration _connectionConfiguration;
        readonly dynamic _state;

        public PrincipalChannelAgent(ConnectionConfiguration connectionConfiguration) {
            _behaviour               = new Behavior(Unstarted);
            _connectionConfiguration = connectionConfiguration ?? throw new ArgumentNullException(nameof(connectionConfiguration));
            _state                   = new ExpandoObject();
        }

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Unstarted(IContext context) {
            switch (context.Message) {
                case Started _: {
                    _behaviour.Become(AwaitConnectionStart);
                    break;
                }
            }
            return Done;
        }

        Task AwaitConnectionStart(IContext context) {
            switch (context.Message) {
                case (Inbound, ConnectionStart message): {
                    _state.HandshakeAgent = context.SpawnNamed(
                        name: "handshake",
                        props: Props.FromProducer(() => new HandshakeAgent(context.Self, _connectionConfiguration))
                            .WithContextDecorator(LoggingContextDecorator.Create)
                    );
                    context.Forward(_state.HandshakeAgent);
                    _behaviour.Become(Negotiating);
                    break;
                }
            }
            return Done;
        }

        Task Negotiating(IContext context) {
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
                    _behaviour.Become(Open);
                    return Done;
                }
                case (AuthenticationFailed): {
                    context.Self.Stop(); // TODO: fail gracefully
                    return Done;
                }
            }
            return Done;
        }

        Task Open(IContext context) {
            switch (context.Message) {
                case (Inbound, RawFrame frame) when frame.Type == FrameType.Heartbeat: {
                    context.Forward(_state.HeartbeatAgent);
                    return Done;
                }
                case (Outbound, _): {
                    context.Forward(context.Parent);
                    return Done;
                }
                case (Inbound, ConnectionClose message): {
                    context.Send(context.Parent, (Outbound, new ConnectionCloseOk()));
                    context.Self.Stop();
                    _behaviour.Become(Closed);
                    return Done;
                }
            }
            return Done;
        }

        Task Closed(IContext context) => Done;
    }
}
