namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Proto;
    using Proto.Mailbox;
    using Xbehave;
    using Xunit;

    public class HeartbeatAgentTests {
        readonly RootContext _rootContext;
        readonly IList<Object> _sent;
        readonly PID _listener;
        readonly PID _subject;

        public HeartbeatAgentTests() {
            _rootContext = new RootContext();
            _sent        = new List<Object>();
            _listener    = _rootContext.Spawn(Props.FromFunc(_ => Actor.Done));
            _subject     = _rootContext.Spawn(
                Props.FromProducer(() => new HeartbeatAgent(_listener))
                    .WithDispatcher(new SynchronousDispatcher())
                    .WithSenderMiddleware(next => (context, target, envelope) => {
                        _sent.Add(envelope.Message);
                        return next(context, target, envelope);
                    })
            );
        }

        [Scenario]
        public void SendsHeartbeatFrames() {
            "When the agent is commanded to begin transmitting heartbeats every 1s".x(() => {
                _rootContext.Send(_subject, (":start-heartbeat-transmission", (UInt16)1));
            });
            "And we wait for 3s".x(async () => {
                await Task.Delay(3100);
            });
            "Then 3 heartbeat frames should have been transmitted".x(() => {
                Assert.Equal(expected: 3, actual: _sent.Where(message => message switch {
                    (":transmit", _) => true,
                    _ => false
                }).Count());
            });
        }

        [Scenario]
        public void DetectsRemoteFlatline() {
            "When the agent is command to begin transmitting heartbeats every 1s".x(() => {
                _rootContext.Send(_subject, (":start-heartbeat-transmission", (UInt16)1));
            });
            "And we wait for 3 missed remote heartbeats".x(async () => {
                await Task.Delay(5000);
            });
            "Then a remote-flatline message should have been sent".x(() => {
                Assert.Contains(_sent, message => message switch {
                    (":remote-flatline", _) => true,
                    _ => false
                });
            });
        }
    }
}
