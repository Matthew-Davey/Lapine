namespace Lapine.Agents {
    using System;
    using System.Dynamic;
    using System.Threading.Tasks;
    using Lapine.Protocol.Commands;
    using Proto;

    using static System.Math;
    using static System.Text.Encoding;
    using static Lapine.Agents.Messages;
    using static Proto.Actor;

    public class HandshakeAgent : IActor {
        readonly PID _listener;
        readonly ConnectionConfiguration _connectionConfiguration;
        readonly Behavior _behaviour;
        readonly dynamic _state;

        public HandshakeAgent(PID listener, ConnectionConfiguration connectionConfiguration) {
            _listener                = listener ?? throw new ArgumentNullException(nameof(listener));
            _connectionConfiguration = connectionConfiguration ?? throw new ArgumentNullException(nameof(connectionConfiguration));
            _behaviour               = new Behavior(AwaitStart);
            _state                   = new ExpandoObject();
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
                    if (!message.Mechanisms.Contains(_connectionConfiguration.AuthenticationStrategy.Mechanism)) {
                        context.Send(_listener, (HandshakeFailed));
                        return context.Self.StopAsync();
                    }

                    _state.AuthenticationStage = 0;
                    var authenticationResponse = _connectionConfiguration.AuthenticationStrategy.Respond((Byte)_state.AuthenticationStage, new Byte[0]);

                    // TODO: Verify protocol version compatibility...
                    context.Send(_listener, (Outbound, new ConnectionStartOk(
                        peerProperties: _connectionConfiguration.PeerProperties.ToDictionary(),
                        mechanism     : _connectionConfiguration.AuthenticationStrategy.Mechanism,
                        response      : UTF8.GetString(authenticationResponse),
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
                case (Inbound, ConnectionSecure message): {
                    _state.AuthenticationStage++;
                    var challenge = UTF8.GetBytes(message.Challenge);
                    var authenticationResponse = _connectionConfiguration.AuthenticationStrategy.Respond(stage: _state.AuthenticationStage, challenge: challenge);
                    context.Send(_listener, (Outbound, new ConnectionSecureOk(UTF8.GetString(authenticationResponse))));
                    return Done;
                }
                case (Inbound, ConnectionTune message): {
                    var heartbeatFrequency  = Min(message.Heartbeat, _connectionConfiguration.HeartbeatFrequency);
                    var maximumFrameSize    = Min(message.FrameMax, _connectionConfiguration.MaximumFrameSize);
                    var maximumChannelCount = Min(message.ChannelMax, _connectionConfiguration.MaximumChannelCount);

                    context.Send(_listener, (Outbound, new ConnectionTuneOk(
                        channelMax: maximumChannelCount,
                        frameMax  : maximumFrameSize,
                        heartbeat : heartbeatFrequency
                    )));
                    context.Send(_listener, (StartHeartbeatTransmission, frequency: heartbeatFrequency));
                    context.Send(_listener, (Outbound, new ConnectionOpen(
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
                    context.Send(_listener, (HandshakeCompleted));
                    context.Self.Stop();
                    return Done;
                }
                default: return Done;
            }
        }
    }
}
