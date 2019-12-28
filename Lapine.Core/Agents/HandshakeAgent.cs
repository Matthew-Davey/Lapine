namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Lapine.Agents.Events;
    using Lapine.Protocol.Commands;
    using Proto;

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
                case ConnectionStart message: {
                    // TODO: Verify protocol version compatibility...
                    context.Send(context.Parent, new ConnectionStartOk(
                        peerProperties: new Dictionary<String, Object>(), // TODO: populate peer properties
                        mechanism     : message.Mechanisms.First(), // TODO: select best auth mechanism
                        response      : String.Empty,
                        locale        : message.Locales.First() // TODO: select best locale
                    ));
                    _behaviour.Become(AwaitConnectionSecure);
                    return Done;
                }
                default: return Done;
            }
        }

        Task AwaitConnectionSecure(IContext context) {
            switch (context.Message) {
                case ConnectionSecure message: {
                    context.Send(context.Parent, new ConnectionSecureOk(
                        response: "guest\0guest\0" // TODO: derive proper auth response
                    ));
                    _behaviour.Become(AwaitConnectionTune);
                    return Done;
                }
                default: return Done;
            }
        }

        Task AwaitConnectionTune(IContext context) {
            switch (context.Message) {
                case ConnectionSecure _: {
                    context.Send(context.Parent, new AuthenticationFailed());
                    context.Self.Stop();
                    return Done;
                }
                case ConnectionTune message: {
                    // TODO: negotiate tuning params
                    context.Send(context.Parent, new ConnectionTuneOk(
                        channelMax: message.ChannelMax,
                        frameMax  : message.FrameMax,
                        heartbeat : message.Heartbeat
                    ));
                    context.Send(context.Parent, new ConnectionOpen(
                        virtualHost: _virtualHost
                    ));
                    _behaviour.Become(AwaitConnectionOpenOk);
                    return Done;
                }
                default: return Done;
            }
        }

        Task AwaitConnectionOpenOk(IContext context) {
            switch (context.Message) {
                case ConnectionOpenOk message: {
                    context.Send(context.Parent, new HandshakeCompleted());
                    context.Self.Stop();
                    return Done;
                }
                default: return Done;
            }
        }
    }
}
