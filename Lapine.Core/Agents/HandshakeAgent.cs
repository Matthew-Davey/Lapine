namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lapine.Protocol.Commands;
    using Proto;

    using static Lapine.Agents.Events;
    using static Lapine.Direction;
    using static Proto.Actor;

    public class HandshakeAgent : IActor {
        readonly Behavior _behaviour;
        readonly String _virtualHost;

        public HandshakeAgent(String virtualHost) {
            _behaviour   = new Behavior(AwaitStart);
            _virtualHost = virtualHost ?? throw new ArgumentNullException(nameof(virtualHost));
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
                        peerProperties: new Dictionary<String, Object>(), // TODO: populate peer properties
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
                    context.Send(context.Parent, (Outbound, new ConnectionOpen(
                        virtualHost: _virtualHost
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
                    return Done;
                }
                default: return Done;
            }
        }
    }
}
