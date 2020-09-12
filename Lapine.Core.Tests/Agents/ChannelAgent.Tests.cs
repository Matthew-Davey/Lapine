namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using Lapine.Protocol.Commands;
    using Proto;
    using Proto.Mailbox;
    using Xbehave;
    using Xunit;

    public class ChannelAgentTests {
        readonly RootContext _rootContext;
        readonly IList<Object> _sent;
        readonly PID _listener;
        readonly PID _subject;

        public ChannelAgentTests() {
            _rootContext = ActorSystem.Default.Root;
            _sent        = new List<Object>();
            _listener    = _rootContext.Spawn(Props.FromFunc(_ => Actor.Done));
            _subject     = _rootContext.Spawn(
                Props.FromProducer(() => new ChannelAgent(_listener))
                    .WithDispatcher(new SynchronousDispatcher())
                    .WithSenderMiddleware(next => (context, target, envelope) => {
                        _sent.Add(envelope.Message);
                        return next(context, target, envelope);
                    })
            );
        }

        [Scenario]
        public void OpeningChannel() {
            "When the agent receives an 'open' message".x(() => {
                _rootContext.Send(_subject, (":open", _listener));
            });
            "Then it should transmit a ChannelOpen command".x(() => {
                Assert.Contains(_sent, message => message switch {
                    (":transmit", ChannelOpen _) => true,
                    _ => false
                });
            });
            "When the agent receives a ChannelOpenOk command".x(() => {
                _rootContext.Send(_subject, (":receive", new ChannelOpenOk()));
            });
            "Then it should send a 'channel-opened' message".x(() => {
                Assert.Contains(_sent, message => message switch {
                    (":channel-opened", PID _) => true,
                    _ => false
                });
            });
        }
    }
}
