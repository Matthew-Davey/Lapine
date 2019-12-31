namespace Lapine.Agents {
    using System;
    using System.Threading.Tasks;
    using Lapine.Protocol.Commands;
    using Proto;

    using static Lapine.Agents.Messages;
    using static Proto.Actor;

    public class HandshakeAgent : IActor {
        readonly Behavior _behaviour;
        readonly ConnectionConfiguration _connectionConfiguration;

        public HandshakeAgent(ConnectionConfiguration connectionConfiguration) {
            _behaviour               = new Behavior(AwaitStart);
            _connectionConfiguration = connectionConfiguration ?? throw new ArgumentNullException(nameof(connectionConfiguration));
        }

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task AwaitStart(IContext context) {
            switch (context.Message) {
                case Started _: {
                    _behaviour.Become(AwaitConnectionStart);
                    return Done;
                }
                default: return Done;
            }
        }

        Task AwaitConnectionStart(IContext context) {
            switch (context.Message) {
                case (Inbound, ConnectionStart message): {
                    // TODO: Verify protocol version compatibility...
                    context.Send(context.Parent, (Outbound, new ConnectionStartOk(
                        peerProperties: _connectionConfiguration.PeerProperties.ToDictionary(),
                        mechanism     : "PLAIN",
                        response      : "\0guest\0guest",
                        locale        : "en_US"
                    )));
                    _behaviour.Become(AwaitConnectionTune);
                    return Done;
                }
                default: return Done;
            }
        }

        Task AwaitConnectionTune(IContext context) {
            switch (context.Message) {
                case (Inbound, ConnectionTune message): {
                    // TODO: negotiate tuning params
                    context.Send(context.Parent, (Outbound, new ConnectionTuneOk(
                        channelMax: message.ChannelMax,
                        frameMax  : message.FrameMax,
                        heartbeat : message.Heartbeat
                    )));
                    context.Send(context.Parent, (StartHeartbeatTransmission, frequency: message.Heartbeat));
                    context.Send(context.Parent, (Outbound, new ConnectionOpen(
                        virtualHost: _connectionConfiguration.VirtualHost
                    )));
                    _behaviour.Become(AwaitConnectionOpenOk);
                    return Done;
                }
                default: return Done;
            }
        }

        Task AwaitConnectionOpenOk(IContext context) {
            switch (context.Message) {
                case (Inbound, ConnectionOpenOk message): {
                    context.Send(context.Parent, (HandshakeCompleted));
                    context.Self.Stop();
                    return Done;
                }
                default: return Done;
            }
        }
    }
}
