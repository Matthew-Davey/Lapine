namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lapine.Protocol.Commands;
    using Proto;

    public class ConnectionAgent : IActor {
        readonly Behavior _behaviour;
        readonly ISet<String> _availableAuthMechanisms;
        readonly ISet<String> _availableLocales;
        readonly String _virtualHost;

        public ConnectionAgent(String virtualHost) {
            _behaviour               = new Behavior(AwaitStart);
            _availableAuthMechanisms = new HashSet<String>();
            _availableLocales        = new HashSet<String>();
            _virtualHost             = virtualHost ?? throw new ArgumentNullException(nameof(virtualHost));
        }

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task AwaitStart(IContext context) {
            switch (context.Message) {
                case Started _: {
                    // TODO: Send protocol header...
                    _behaviour.Become(AwaitConnectionStart);
                    return Actor.Done;
                }
                default: return Actor.Done;
            }
        }

        Task AwaitConnectionStart(IContext context) {
            switch (context.Message) {
                // TODO: case ProtocolHeader = server reject
                case ConnectionStart message: {
                    // TODO: Verify protocol version compatibility...
                    _availableAuthMechanisms.AddRange(message.Mechanisms);
                    _availableLocales.AddRange(message.Locales);
                    // TODO: Send ConnectionStartOk...
                    _behaviour.Become(AwaitAuthentication);
                    return Actor.Done;
                }
                default: return Actor.Done;
            }
        }

        Task AwaitAuthentication(IContext context) {
            switch (context.Message) {
                case ConnectionSecure message: {
                    // TODO: Send ConnectionSecureOk...
                    _behaviour.Become(AwaitTune);
                    return Actor.Done;
                }
                default: return Actor.Done;
            }
        }

        Task AwaitTune(IContext context) {
            switch (context.Message) {
                // TODO: case ConnectionSecure = auth failure...
                case ConnectionTune message: {
                    // TODO: Send ConnectionTuneOk...
                    // TODO: Send ConnectionOpen...
                    _behaviour.Become(AwaitConnectionOpen);
                    return Actor.Done;
                }
                default: return Actor.Done;
            }
        }

        Task AwaitConnectionOpen(IContext context) {
            switch (context.Message) {
                case ConnectionOpenOk message: {
                    // TODO: complete handshake...
                    return Actor.Done;
                }
                default: return Actor.Done;
            }
        }
    }
}
