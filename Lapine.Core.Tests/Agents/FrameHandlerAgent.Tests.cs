namespace Lapine.Agents {
    using System;
    using System.Linq;
    using System.Threading;
    using Lapine.Agents.Events;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;
    using Xunit;

    [Collection("Agents")]
    public class FrameHandlerAgentTests {
        readonly RootContext _context;
        readonly PID _subject;

        public FrameHandlerAgentTests() {
            _context = new RootContext();
            _subject = _context.Spawn(Props.FromProducer(() => new FrameHandlerAgent()));
        }

        [Fact]
        public void HandlesConnectionStartMethodFrame() {
            var receivedCommand = default(ConnectionStart);
            var receivedEvent = new ManualResetEventSlim();

            var subscription = Actor.EventStream.Subscribe<ConnectionStart>(message => {
                receivedCommand = message;
                receivedEvent.Set();
            });

            var command = new ConnectionStart(
                version: (0, 9),
                serverProperties: "\aproductS\0\0\0\bRabbitMQ\aversionS\0\0\0\u00053.8.1",
                mechanisms: new [] { "PLAIN", "AMQPLAIN" },
                locales: new [] { "en_US" }
            );

            Actor.EventStream.Publish(new FrameReceived(RawFrame.Wrap(channel: 0, command)));

            if (receivedEvent.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.Equal(expected: command.Locales.ToArray(), actual: receivedCommand.Locales.ToArray());
                Assert.Equal(expected: command.Mechanisms.ToArray(), actual: receivedCommand.Mechanisms.ToArray());
                Assert.Equal(expected: command.ServerProperties, actual: receivedCommand.ServerProperties);
                Assert.Equal(expected: command.Version, actual: receivedCommand.Version);
            }
            else {
                // No `ConnectionStart` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled was received");
            }
        }
    }
}
