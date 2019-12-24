namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Lapine.Agents.Events;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Bogus;
    using Proto;
    using Xunit;

    [Collection("Agents")]
    public class FrameHandlerAgentTests : Faker {
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
                serverProperties: new Dictionary<String, Object> {
                    ["product"] = "RabbitMQ",
                    ["version"] = "3.8.1"
                },
                mechanisms: new [] { "PLAIN", "AMQPLAIN" },
                locales: new [] { "en_US" }
            );

            Actor.EventStream.Publish(new FrameReceived(RawFrame.Wrap(channel: 0, command)));

            if (receivedEvent.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.Equal(expected: command.Locales.ToArray(), actual: receivedCommand.Locales.ToArray());
                Assert.Equal(expected: command.Mechanisms.ToArray(), actual: receivedCommand.Mechanisms.ToArray());
                Assert.Equal(expected: command.ServerProperties.ToArray(), actual: receivedCommand.ServerProperties.ToArray());
                Assert.Equal(expected: command.Version, actual: receivedCommand.Version);
            }
            else {
                // No `ConnectionStart` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }

        [Fact]
        public void HandlesConnectionSecureMethodFrame() {
            var receivedCommand = default(ConnectionSecure);
            var receivedEvent = new ManualResetEventSlim();

            var subscription = Actor.EventStream.Subscribe<ConnectionSecure>(message => {
                receivedCommand = message;
                receivedEvent.Set();
            });

            var command = new ConnectionSecure(challenge: Random.Hash());

            Actor.EventStream.Publish(new FrameReceived(RawFrame.Wrap(channel: 0, command)));

            if (receivedEvent.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.Equal(expected: command.Challenge, actual: receivedCommand.Challenge);
            }
            else {
                // No `ConnectionSecure` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }

        [Fact]
        public void HandlesConnectionTuneMethodFrame() {
            var receivedCommand = default(ConnectionTune);
            var receivedEvent = new ManualResetEventSlim();

            var subscription = Actor.EventStream.Subscribe<ConnectionTune>(message => {
                receivedCommand = message;
                receivedEvent.Set();
            });

            var command = new ConnectionTune(
                channelMax: Random.UShort(),
                frameMax  : Random.UInt(),
                heartbeat : Random.UShort()
            );

            Actor.EventStream.Publish(new FrameReceived(RawFrame.Wrap(channel: 0, command)));

            if (receivedEvent.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.Equal(expected: command.ChannelMax, actual: receivedCommand.ChannelMax);
                Assert.Equal(expected: command.FrameMax, actual: receivedCommand.FrameMax);
                Assert.Equal(expected: command.Heartbeat, actual: receivedCommand.Heartbeat);
            }
            else {
                // No `ConnectionTune` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }

        [Fact]
        public void HandlesConnectionOpenOkMethodFrame() {
            var receivedEvent = new ManualResetEventSlim();
            var subscription  = Actor.EventStream.Subscribe<ConnectionOpenOk>(_ => receivedEvent.Set());

            Actor.EventStream.Publish(new FrameReceived(RawFrame.Wrap(channel: 0, new ConnectionOpenOk())));

            if (receivedEvent.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(receivedEvent.IsSet);
            }
            else {
                // No `ConnectionOpenOk` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }

        [Fact]
        public void HandlesConnectionCloseOkMethodFrame() {
            var receivedEvent = new ManualResetEventSlim();
            var subscription = Actor.EventStream.Subscribe<ConnectionCloseOk>(_ => receivedEvent.Set());

            Actor.EventStream.Publish(new FrameReceived(RawFrame.Wrap(channel: 0, new ConnectionCloseOk())));

            if (receivedEvent.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(receivedEvent.IsSet);
            }
            else {
                // No `ConnectionCloseOk` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }
    }
}
