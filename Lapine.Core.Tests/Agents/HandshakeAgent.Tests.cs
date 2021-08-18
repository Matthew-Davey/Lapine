namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using Lapine.Client;
    using Lapine.Protocol.Commands;
    using Proto;
    using Proto.Mailbox;
    using Xbehave;
    using Xunit;

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.DispatcherAgent.Protocol;
    using static Lapine.Agents.HandshakeAgent.Protocol;

    public class HandshakeAgentTests {
        readonly ActorSystem _system;
        readonly RootContext _rootContext;
        readonly IList<Object> _sent;
        readonly PID _listener;
        readonly PID _subject;

        public HandshakeAgentTests() {
            _system      = new ActorSystem();
            _rootContext = _system.Root;
            _sent        = new List<Object>();
            _listener    = _rootContext.Spawn(Props.FromFunc(_ => CompletedTask));
            _subject     = _rootContext.Spawn(
                HandshakeAgent.Create()
                    .WithDispatcher(new SynchronousDispatcher())
                    .WithSenderMiddleware(next => (context, target, envelope) => {
                        _sent.Add(envelope.Message);
                        return next(context, target, envelope);
                    })
            );
        }

        [Scenario]
        public void FailsWhenAuthMechanismNotSupportedByServer() {
            "Given the agent has started the handshake process".x(() => {
                _rootContext.Send(_subject, new BeginHandshake(ConnectionConfiguration.Default, _listener, _listener));
            });
            "When the agent receives a ConnectionStart command with an unsupported auth mechanism".x(() => {
                _rootContext.Send(_subject, new ConnectionStart(
                    Version         : (0, 9),
                    ServerProperties: new Dictionary<String, Object>(),
                    Mechanisms      : new [] { "unsupported" },
                    Locales         : new [] { "en_US" }
                ));
            });
            "Then it should publish a handshake failed event".x(() => {
                Assert.Contains(_sent, message => message switch {
                    HandshakeFailed _ => true,
                    _                 => false
                });
            });
        }

        [Scenario]
        public void FailsWhenLocaleNotSupportedByServer() {
            "Given the agent has started the handshake process".x(() => {
                _rootContext.Send(_subject, new BeginHandshake(ConnectionConfiguration.Default, _listener, _listener));
            });
            "When the agent receives a ConnectionStart message with an unsupported locale".x(() => {
                _rootContext.Send(_subject, new ConnectionStart(
                    Version         : (0, 9),
                    ServerProperties: new Dictionary<String, Object>(),
                    Mechanisms      : new [] { "PLAIN" },
                    Locales         : new [] { "unsupported" }
                ));
            });
            "Then it should publish a handshake failed event".x(() => {
                Assert.Contains(_sent, message => message switch {
                    HandshakeFailed _ => true,
                    _                 => false
                });
            });
        }

        [Scenario]
        public void StartsConnection() {
            "Given the agent has started the handshake process".x(() => {
                _rootContext.Send(_subject, new BeginHandshake(ConnectionConfiguration.Default, _listener, _listener));
            });
            "When the agent receives a ConnectionStart message with a supported auth mechanism".x(() => {
                _rootContext.Send(_subject, new ConnectionStart(
                    Version         : (0, 9),
                    ServerProperties: new Dictionary<String, Object>(),
                    Mechanisms      : new [] { "PLAIN" },
                    Locales         : new [] { "en_US" }
                ));
            });
            "Then it should send a ConnectionStartOk message".x(() => {
                Assert.Contains(_sent, message => message switch {
                    Dispatch { Entity: ConnectionStartOk } => true,
                    _ => false
                });
            });
        }

        [Scenario]
        public void TunesConnection() {
            "Given the agent has started the handshake process".x(() => {
                _rootContext.Send(_subject, new BeginHandshake(ConnectionConfiguration.Default, _listener, _listener));
            });
            "And the connection has already been started".x(() => {
                _rootContext.Send(_subject, new ConnectionStart(
                    Version         : (0, 9),
                    ServerProperties: new Dictionary<String, Object>(),
                    Mechanisms      : new [] { "PLAIN" },
                    Locales         : new [] { "en_US" }
                ));
            });
            "When the agent receives a ConnectionTune message".x(() => {
                _rootContext.Send(_subject, new ConnectionTune(
                    ChannelMax: ConnectionConfiguration.DefaultMaximumChannelCount,
                    FrameMax  : ConnectionConfiguration.DefaultMaximumFrameSize,
                    Heartbeat : 60
                ));
            });
            "Then it should send a ConnectionTuneOk message".x(() => {
                Assert.Contains(_sent, message => message switch {
                    Dispatch { Entity: ConnectionTuneOk } => true,
                    _ => false
                });
            });
            "And it should send a ConnectionOpen message".x(() => {
                Assert.Contains(_sent, message => message switch {
                    Dispatch { Entity: ConnectionOpen { VirtualHost: ConnectionConfiguration.DefaultVirtualHost } } => true,
                    _ => false
                });
            });
        }

        [Scenario]
        public void CompletesHandshake() {
            "Given the agent has started the handshake process".x(() => {
                _rootContext.Send(_subject, new BeginHandshake(ConnectionConfiguration.Default, _listener, _listener));
            });
            "And the connection has already been started and tuned".x(() => {
                _rootContext.Send(_subject, new ConnectionStart(
                    Version         : (0, 9),
                    ServerProperties: new Dictionary<String, Object>(),
                    Mechanisms      : new [] { "PLAIN" },
                    Locales         : new [] { "en_US" }
                ));
                _rootContext.Send(_subject, new ConnectionTune(
                    ChannelMax: ConnectionConfiguration.DefaultMaximumChannelCount,
                    FrameMax  : ConnectionConfiguration.DefaultMaximumFrameSize,
                    Heartbeat : 60
                ));
            });
            "When the agent receives a ConnectionOpenOk message".x(() => {
                _rootContext.Send(_subject, new ConnectionOpenOk());
            });
            "Then it should complete the handshake process".x(() => {
                Assert.Contains(_sent, message => message switch {
                    HandshakeCompleted _ => true,
                    _                    => false
                });
            });
        }
    }
}
