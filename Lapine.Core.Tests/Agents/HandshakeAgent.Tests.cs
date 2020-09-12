namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using Lapine.Protocol.Commands;
    using Proto;
    using Proto.Mailbox;
    using Xbehave;
    using Xunit;

    public class HandshakeAgentTests {
        readonly RootContext _rootContext;
        readonly IList<Object> _sent;
        readonly PID _listener;
        readonly PID _subject;

        public HandshakeAgentTests() {
            _rootContext = ActorSystem.Default.Root;
            _sent        = new List<Object>();
            _listener    = _rootContext.Spawn(Props.FromFunc(_ => Actor.Done));
            _subject     = _rootContext.Spawn(
                Props.FromProducer(() => new HandshakeAgent(_listener, ConnectionConfiguration.Default))
                    .WithDispatcher(new SynchronousDispatcher())
                    .WithSenderMiddleware(next => (context, target, envelope) => {
                        _sent.Add(envelope.Message);
                        return next(context, target, envelope);
                    })
            );
        }

        [Scenario]
        public void FailsWhenAuthMechanismNotSupportedByServer() {
            "When the agent receives a ConnectionStart command with an unsupported auth mechanism".x(() => {
                _rootContext.Send(_subject, (":receive", new ConnectionStart(
                    version         : (0, 9),
                    serverProperties: new Dictionary<String, Object>(),
                    mechanisms      : new [] { "unsupported" },
                    locales         : new [] { "en_US" }
                )));
            });
            "Then it should send a handshake failed message".x(() => {
                Assert.Contains(_sent, message => message switch {
                    (":handshake-failed") => true,
                    _                     => false
                });
            });
        }

        [Scenario]
        public void FailsWhenLocaleNotSupportedByServer() {
            "When the agent receives a ConnectionStart message with an unsupported locale".x(() => {
                _rootContext.Send(_subject, (":receive", new ConnectionStart(
                    version         : (0, 9),
                    serverProperties: new Dictionary<String, Object>(),
                    mechanisms      : new [] { "PLAIN" },
                    locales         : new [] { "unsupported" }
                )));
            });
            "Then it should send a handshake failed message".x(() => {
                Assert.Contains(_sent, message => message switch {
                    (":handshake-failed") => true,
                    _                     => false
                });
            });
        }

        [Scenario]
        public void StartsConnection() {
            "When the agent receives a ConnectionStart message with a supported auth mechanism".x(() => {
                _rootContext.Send(_subject, (":receive", new ConnectionStart(
                    version         : (0, 9),
                    serverProperties: new Dictionary<String, Object>(),
                    mechanisms      : new [] { "PLAIN" },
                    locales         : new [] { "en_US" }
                )));
            });
            "Then it should send a ConnectionStartOk message".x(() => {
                Assert.Contains(_sent, message => message switch {
                    (":transmit", ConnectionStartOk _) => true,
                    _                                  => false
                });
            });
        }

        [Scenario]
        public void TunesConnection() {
            "Given the connection has already been started".x(() => {
                _rootContext.Send(_subject, (":receive", new ConnectionStart(
                    version         : (0, 9),
                    serverProperties: new Dictionary<String, Object>(),
                    mechanisms      : new [] { "PLAIN" },
                    locales         : new [] { "en_US" }
                )));
            });
            "When the agent receives a ConnectionTune message".x(() => {
                _rootContext.Send(_subject, (":receive", new ConnectionTune(
                    channelMax: ConnectionConfiguration.DefaultMaximumChannelCount,
                    frameMax  : ConnectionConfiguration.DefaultMaximumFrameSize,
                    heartbeat : ConnectionConfiguration.DefaultHeartbeatFrequency
                )));
            });
            "Then it should send a ConnectionTuneOk message".x(() => {
                Assert.Contains(_sent, message => message switch {
                    (":transmit", ConnectionTuneOk _) => true,
                    _                                 => false
                });
            });
            "And it should sent a ConnectionOpen message".x(() => {
                Assert.Contains(_sent, message => message switch {
                    (":transmit", ConnectionOpen x) when x.VirtualHost == ConnectionConfiguration.DefaultVirtualHost => true,
                    _ => false
                });
            });
        }

        [Scenario]
        public void CompletesHandshake() {
            "Given the connection has already been started and tuned".x(() => {
                _rootContext.Send(_subject, (":receive", new ConnectionStart(
                    version         : (0, 9),
                    serverProperties: new Dictionary<String, Object>(),
                    mechanisms      : new [] { "PLAIN" },
                    locales         : new [] { "en_US" }
                )));
                _rootContext.Send(_subject, (":receive", new ConnectionTune(
                    channelMax: ConnectionConfiguration.DefaultMaximumChannelCount,
                    frameMax  : ConnectionConfiguration.DefaultMaximumFrameSize,
                    heartbeat : ConnectionConfiguration.DefaultHeartbeatFrequency
                )));
            });
            "When the agent receives a ConnectionOpenOk message".x(() => {
                _rootContext.Send(_subject, (":receive", new ConnectionOpenOk()));
            });
            "Then it should complete the handshake process".x(() => {
                Assert.Contains(_sent, message => message switch {
                    (":handshake-completed") => true,
                    _                        => false
                });
            });
        }
    }
}
