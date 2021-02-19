namespace Lapine.Agents {
    using System;
    using System.Dynamic;
    using System.Threading.Tasks;
    using Lapine.Protocol.Commands;
    using Proto;
    using Proto.Schedulers.SimpleScheduler;

    using static System.Math;
    using static System.Text.Encoding;
    using static Proto.Actor;

    class HandshakeAgent : IActor {
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
                    _state.TimeoutScheduler = new SimpleScheduler(context);
                    _state.TimeoutScheduler.ScheduleTellOnce(
                        delay  : TimeSpan.FromMilliseconds(_connectionConfiguration.ConnectionTimeout),
                        target : context.Self,
                        message: (":timeout")
                    );
                    return Done;
                }
                default: return Done;
            }
        }

        Task AwaitConnectionStart(IContext context) {
            switch (context.Message) {
                case (":timeout"): {
                    context.Send(_listener, (":handshake-failed"));
                    context.Stop(context.Self!);
                    return Done;
                }
                case (":receive", ConnectionStart message): {
                    if (!message.Mechanisms.Contains(_connectionConfiguration.AuthenticationStrategy.Mechanism)) {
                        context.Send(_listener, (":handshake-failed"));
                        context.Stop(context.Self!);
                        return Done;
                    }

                    if (!message.Locales.Contains(_connectionConfiguration.Locale)) {
                        context.Send(_listener, (":handshake-failed"));
                        context.Stop(context.Self!);
                        return Done;
                    }

                    // TODO: Verify protocol version compatibility...

                    _state.ServerProperties = message.ServerProperties;
                    _state.AuthenticationStage = 0;
                    var authenticationResponse = _connectionConfiguration.AuthenticationStrategy.Respond((Byte)_state.AuthenticationStage, new Byte[0]);

                    context.Send(_listener, (":transmit", new ConnectionStartOk(
                        peerProperties: _connectionConfiguration.PeerProperties.ToDictionary(),
                        mechanism     : _connectionConfiguration.AuthenticationStrategy.Mechanism,
                        response      : UTF8.GetString(authenticationResponse),
                        locale        : _connectionConfiguration.Locale
                    )));
                    _behaviour.Become(AwaitConnectionSecureOrTune);
                    return Done;
                }
                default: return Done;
            }
        }

        Task AwaitConnectionSecureOrTune(IContext context) {
            switch (context.Message) {
                case (":timeout"): {
                    context.Send(_listener, (":handshake-failed"));
                    context.Stop(context.Self!);
                    return Done;
                }
                case (":receive", ConnectionSecure message): {
                    _state.AuthenticationStage++;
                    var challenge = UTF8.GetBytes(message.Challenge);
                    var authenticationResponse = _connectionConfiguration.AuthenticationStrategy.Respond(stage: _state.AuthenticationStage, challenge: challenge);
                    context.Send(_listener, (":transmit", new ConnectionSecureOk(UTF8.GetString(authenticationResponse))));
                    return Done;
                }
                case (":receive", ConnectionTune message): {
                    _state.HeartbeatFrequency  = Min(message.Heartbeat, _connectionConfiguration.HeartbeatFrequency);
                    _state.MaximumFrameSize    = Min(message.FrameMax, _connectionConfiguration.MaximumFrameSize);
                    _state.MaximumChannelCount = Min(message.ChannelMax, _connectionConfiguration.MaximumChannelCount);

                    context.Send(_listener, (":transmit", new ConnectionTuneOk(
                        channelMax: (UInt16)_state.MaximumChannelCount,
                        frameMax  : (UInt32)_state.MaximumFrameSize,
                        heartbeat : (UInt16)_state.HeartbeatFrequency
                    )));
                    context.Send(_listener, (":start-heartbeat-transmission", frequency: _state.HeartbeatFrequency));
                    context.Send(_listener, (":transmit", new ConnectionOpen(
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
                case (":timeout"): {
                    context.Send(_listener, (":handshake-failed"));
                    context.Stop(context.Self!);
                    return Done;
                }
                case (":receive", ConnectionOpenOk _): {
                    context.Send(_listener, (":handshake-completed", (UInt16)_state.MaximumChannelCount));
                    context.Stop(context.Self!);
                    return Done;
                }
                default: return Done;
            }
        }
    }
}
