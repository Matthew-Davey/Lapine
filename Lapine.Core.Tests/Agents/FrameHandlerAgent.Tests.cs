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
    using Proto.Mailbox;
    using Xunit;

    [Collection("Agents")]
    public class FrameHandlerAgentTests : Faker {
        readonly RootContext _context;
        readonly ManualResetEventSlim _messageReceivedSignal;
        readonly PID _listener;
        readonly PID _subject;

        Object _outboundMessage;

        public FrameHandlerAgentTests() {
            _context               = new RootContext();
            _messageReceivedSignal = new ManualResetEventSlim();

            _listener = _context.Spawn(
                Props.FromFunc(context => {
                    switch (context.Message) {
                        case SystemMessage _: {
                            return Actor.Done;
                        }
                        default: {
                            _outboundMessage = context.Message;
                            _messageReceivedSignal.Set();
                            return Actor.Done;
                        }
                    }
                })
            );
            _subject = _context.Spawn(Props.FromProducer(() => new FrameHandlerAgent(_listener)));
        }

        [Fact]
        public void HandlesConnectionStartMethodFrame() {
            var inbound = new ConnectionStart(
                version: (0, 9),
                serverProperties: new Dictionary<String, Object> {
                    ["product"] = "RabbitMQ",
                    ["version"] = "3.8.1"
                },
                mechanisms: new [] { "PLAIN", "AMQPLAIN" },
                locales: new [] { "en_US" }
            );

            _context.Send(_subject, new FrameReceived(RawFrame.Wrap(channel: 0, inbound)));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var outbound = _outboundMessage as ConnectionStart;

                Assert.Equal(expected: inbound.Locales.ToArray(), actual: outbound.Locales.ToArray());
                Assert.Equal(expected: inbound.Mechanisms.ToArray(), actual: outbound.Mechanisms.ToArray());
                Assert.Equal(expected: inbound.ServerProperties.ToArray(), actual: outbound.ServerProperties.ToArray());
                Assert.Equal(expected: inbound.Version, actual: outbound.Version);
            }
            else {
                // No `ConnectionStart` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }

        [Fact]
        public void HandlesConnectionSecureMethodFrame() {
            var inbound = new ConnectionSecure(challenge: Random.Hash());

            _context.Send(_subject, new FrameReceived(RawFrame.Wrap(channel: 0, inbound)));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var outbound = _outboundMessage as ConnectionSecure;
                Assert.Equal(expected: inbound.Challenge, actual: outbound.Challenge);
            }
            else {
                // No `ConnectionSecure` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }

        [Fact]
        public void HandlesConnectionTuneMethodFrame() {
            var inbound = new ConnectionTune(
                channelMax: Random.UShort(),
                frameMax  : Random.UInt(),
                heartbeat : Random.UShort()
            );

            _context.Send(_subject, new FrameReceived(RawFrame.Wrap(channel: 0, inbound)));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var outbound = _outboundMessage as ConnectionTune;
                Assert.Equal(expected: inbound.ChannelMax, actual: outbound.ChannelMax);
                Assert.Equal(expected: inbound.FrameMax, actual: outbound.FrameMax);
                Assert.Equal(expected: inbound.Heartbeat, actual: outbound.Heartbeat);
            }
            else {
                // No `ConnectionTune` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }

        [Fact]
        public void HandlesConnectionOpenOkMethodFrame() {
            _context.Send(_subject, new FrameReceived(RawFrame.Wrap(channel: 0, new ConnectionOpenOk())));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `ConnectionOpenOk` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }

        [Fact]
        public void HandlesConnectionCloseMethodFrame() {
            var inbound = new ConnectionClose(
                replyCode    : Random.UShort(),
                replyText    : Random.Word(),
                failingMethod: (Random.UShort(), Random.UShort())
            );
            _context.Send(_subject, new FrameReceived(RawFrame.Wrap(channel: 0, inbound)));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var outbound = _outboundMessage as ConnectionClose;

                Assert.Equal(expected: inbound.FailingMethod, actual: outbound.FailingMethod);
                Assert.Equal(expected: inbound.ReplyCode, actual: outbound.ReplyCode);
                Assert.Equal(expected: inbound.ReplyText, actual: outbound.ReplyText);
            }
            else {
                // No `ConnectionClose` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }

        [Fact]
        public void HandlesConnectionCloseOkMethodFrame() {
            _context.Send(_subject, new FrameReceived(RawFrame.Wrap(channel: 0, new ConnectionCloseOk())));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `ConnectionCloseOk` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }

        [Fact]
        public void HandlesChannelOpenOkMethodFrame() {
            _context.Send(_subject, new FrameReceived(RawFrame.Wrap(channel: 0, new ChannelOpenOk())));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `ChannelOpenOk` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }

        [Fact]
        public void HandlesChannelFlowMethodFrame() {
            var inbound = new ChannelFlow(
                active: Random.Bool()
            );
            _context.Send(_subject, new FrameReceived(RawFrame.Wrap(channel: 0, inbound)));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var outbound = _outboundMessage as ChannelFlow;

                Assert.Equal(expected: inbound.Active, actual: outbound.Active);
            }
            else {
                // No `ChannelFlow` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }

        [Fact]
        public void HandlesChannelFlowOkMethodFrame() {
            var inbound = new ChannelFlowOk(
                active: Random.Bool()
            );
            _context.Send(_subject, new FrameReceived(RawFrame.Wrap(channel: 0, inbound)));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var outbound = _outboundMessage as ChannelFlowOk;

                Assert.Equal(expected: inbound.Active, actual: outbound.Active);
            }
            else {
                // No `ChannelFlowOk` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }

        [Fact]
        public void HandlesChannelCloseMethodFrame() {
            var inbound = new ChannelClose(
                replyCode    : Random.UShort(),
                replyText    : Random.Word(),
                failingMethod: (Random.UShort(), Random.UShort())
            );
            _context.Send(_subject, new FrameReceived(RawFrame.Wrap(channel: 0, inbound)));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var outbound = _outboundMessage as ChannelClose;

                Assert.Equal(expected: inbound.FailingMethod, actual: outbound.FailingMethod);
                Assert.Equal(expected: inbound.ReplyCode, actual: outbound.ReplyCode);
                Assert.Equal(expected: inbound.ReplyText, actual: outbound.ReplyText);
            }
            else {
                // No `ChannelClose` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }

        [Fact]
        public void HandlesChannelCloseOkMethodFrame() {
            _context.Send(_subject, new FrameReceived(RawFrame.Wrap(channel: 0, new ChannelCloseOk())));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `ChannelCloseOk` command was handled within 100 millis...
                throw new TimeoutException("Timeout occurred before command was handled");
            }
        }
    }
}
