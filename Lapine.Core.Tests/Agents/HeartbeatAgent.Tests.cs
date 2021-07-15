namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Lapine.Protocol;
    using Proto;
    using Proto.Mailbox;
    using Xbehave;
    using Xunit;

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.DispatcherAgent.Protocol;
    using static Lapine.Agents.HeartbeatAgent.Protocol;

    public class HeartbeatAgentTests {
        readonly ActorSystem _system;
        readonly RootContext _rootContext;
        readonly IList<Object> _sent;
        readonly PID _listener;
        readonly PID _subject;

        public HeartbeatAgentTests() {
            _system      = new ActorSystem();
            _rootContext = _system.Root;
            _sent        = new List<Object>();
            _listener    = _rootContext.Spawn(Props.FromFunc(_ => CompletedTask));
            _subject     = _rootContext.Spawn(
                HeartbeatAgent.Create()
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
                _rootContext.Send(_subject, new StartHeartbeat(_listener, TimeSpan.FromMilliseconds(10), _listener));
            });
            "And we wait for 3 heartbeat durations".x(async () => {
                await Task.Delay(36);
            });
            "Then at least 3 heartbeat frames should have been transmitted".x(() => {
                Assert.True(3 <= _sent.Count(message => message switch {
                    Dispatch { Entity: RawFrame { Type: FrameType.Heartbeat } } => true,
                    _ => false
                }));
            });
        }

        [Scenario]
        public void DetectsRemoteFlatline() {
            "When the agent is commanded to begin transmitting heartbeats every 1s".x(() => {
                _rootContext.Send(_subject, new StartHeartbeat(_listener, TimeSpan.FromMilliseconds(10), _listener));
            });
            "And we wait for 3 missed remote heartbeats".x(async () => {
                await Task.Delay(36);
            });
            "Then a remote-flatline message should have been sent".x(() => {
                Assert.Contains(_sent, message => message switch {
                    RemoteFlatline _ => true,
                    _                => false
                });
            });
        }
    }
}
