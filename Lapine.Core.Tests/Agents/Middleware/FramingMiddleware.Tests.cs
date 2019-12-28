namespace Lapine.Agents.Middleware {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Bogus;
    using Proto;
    using Proto.Mailbox;
    using Xunit;

    public class FramingMiddlewareTests : Faker {
        readonly UInt16 _channelNumber;
        readonly ManualResetEventSlim _messageReceivedSignal;
        readonly RootContext _context;
        readonly PID _subject;

        Object _unwrappedMessage;

        public FramingMiddlewareTests() {
            _channelNumber = Random.UShort();
            _messageReceivedSignal = new  ManualResetEventSlim();
            _context = new RootContext();
            _subject = _context.Spawn(
                Props.FromFunc(context => {
                    switch (context.Message) {
                        case SystemMessage _: {
                            return Actor.Done;
                        }
                        default: {
                            _unwrappedMessage = context.Message;
                            _messageReceivedSignal.Set();
                            return Actor.Done;
                        }
                    }
                })
                .WithReceiveMiddleware(FramingMiddleware.UnwrapFrames(_channelNumber))
                .WithSenderMiddleware(FramingMiddleware.WrapCommands(_channelNumber))
            );
        }

        [Fact]
        public void UnwrapsConnectionStartMethodFrame() {
            var message = new ConnectionStart(
                version: (0, 9),
                serverProperties: new Dictionary<String, Object> {
                    ["product"] = "RabbitMQ",
                    ["version"] = "3.8.1"
                },
                mechanisms: new [] { "PLAIN", "AMQPLAIN" },
                locales: new [] { "en_US" }
            );

            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as ConnectionStart;

                Assert.Equal(expected: message.Locales.ToArray(), actual: unwrappedMessage.Locales.ToArray());
                Assert.Equal(expected: message.Mechanisms.ToArray(), actual: unwrappedMessage.Mechanisms.ToArray());
                Assert.Equal(expected: message.ServerProperties.ToArray(), actual: unwrappedMessage.ServerProperties.ToArray());
                Assert.Equal(expected: message.Version, actual: unwrappedMessage.Version);
            }
            else {
                // No `ConnectionStart` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsConnectionSecureMethodFrame() {
            var message = new ConnectionSecure(challenge: Random.Hash());

            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as ConnectionSecure;
                Assert.Equal(expected: message.Challenge, actual: unwrappedMessage.Challenge);
            }
            else {
                // No `ConnectionSecure` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsConnectionTuneMethodFrame() {
            var message = new ConnectionTune(
                channelMax: Random.UShort(),
                frameMax  : Random.UInt(),
                heartbeat : Random.UShort()
            );

            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as ConnectionTune;
                Assert.Equal(expected: message.ChannelMax, actual: unwrappedMessage.ChannelMax);
                Assert.Equal(expected: message.FrameMax, actual: unwrappedMessage.FrameMax);
                Assert.Equal(expected: message.Heartbeat, actual: unwrappedMessage.Heartbeat);
            }
            else {
                // No `ConnectionTune` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsConnectionOpenOkMethodFrame() {
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, new ConnectionOpenOk()));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `ConnectionOpenOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsConnectionCloseMethodFrame() {
            var message = new ConnectionClose(
                replyCode    : Random.UShort(),
                replyText    : Random.Word(),
                failingMethod: (Random.UShort(), Random.UShort())
            );
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as ConnectionClose;

                Assert.Equal(expected: message.FailingMethod, actual: unwrappedMessage.FailingMethod);
                Assert.Equal(expected: message.ReplyCode, actual: unwrappedMessage.ReplyCode);
                Assert.Equal(expected: message.ReplyText, actual: unwrappedMessage.ReplyText);
            }
            else {
                // No `ConnectionClose` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsConnectionCloseOkMethodFrame() {
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, new ConnectionCloseOk()));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `ConnectionCloseOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsChannelOpenOkMethodFrame() {
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, new ChannelOpenOk()));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `ChannelOpenOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsChannelFlowMethodFrame() {
            var message = new ChannelFlow(
                active: Random.Bool()
            );
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as ChannelFlow;

                Assert.Equal(expected: message.Active, actual: unwrappedMessage.Active);
            }
            else {
                // No `ChannelFlow` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsChannelFlowOkMethodFrame() {
            var message = new ChannelFlowOk(
                active: Random.Bool()
            );
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as ChannelFlowOk;

                Assert.Equal(expected: message.Active, actual: unwrappedMessage.Active);
            }
            else {
                // No `ChannelFlowOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsChannelCloseMethodFrame() {
            var message = new ChannelClose(
                replyCode    : Random.UShort(),
                replyText    : Random.Word(),
                failingMethod: (Random.UShort(), Random.UShort())
            );
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as ChannelClose;

                Assert.Equal(expected: message.FailingMethod, actual: unwrappedMessage.FailingMethod);
                Assert.Equal(expected: message.ReplyCode, actual: unwrappedMessage.ReplyCode);
                Assert.Equal(expected: message.ReplyText, actual: unwrappedMessage.ReplyText);
            }
            else {
                // No `ChannelClose` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsChannelCloseOkMethodFrame() {
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, new ChannelCloseOk()));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `ChannelCloseOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsExchangeDeclareOkMethodFrame() {
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, new ExchangeDeclareOk()));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `ExchangeDeclareOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsExchangeDeleteOkMethodFrame() {
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, new ExchangeDeleteOk()));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `ExchangeDeleteOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsQueueDeclareOkMethodFrame() {
            var message = new QueueDeclareOk(
                queueName    : Random.Word(),
                messageCount : Random.UInt(),
                consumerCount: Random.UInt()
            );
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as QueueDeclareOk;

                Assert.Equal(expected: message.ConsumerCount, actual: unwrappedMessage.ConsumerCount);
                Assert.Equal(expected: message.MessageCount, actual: unwrappedMessage.MessageCount);
                Assert.Equal(expected: message.QueueName, actual: unwrappedMessage.QueueName);
            }
            else {
                // No `QueueDeclareOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsQueueBindOkMethodFrame() {
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, new QueueBindOk()));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `QueueBindOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsQueueUnbindOkMethodFrame() {
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, new QueueUnbindOk()));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `QueueUnbindOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsQueuePurgeOkMethodFrame() {
            var message = new QueuePurgeOk(
                messageCount: Random.UInt()
            );
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as QueuePurgeOk;

                Assert.Equal(expected: message.MessageCount, actual: unwrappedMessage.MessageCount);
            }
            else {
                // No `QueuePurgeOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsQueueDeleteOkMethodFrame() {
            var message = new QueueDeleteOk(
                messageCount: Random.UInt()
            );
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as QueueDeleteOk;

                Assert.Equal(expected: message.MessageCount, actual: unwrappedMessage.MessageCount);
            }
            else {
                // No `QueueDeleteOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsBasicQosOkMethodFrame() {
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, new BasicQosOk()));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `BasicQosOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsBasicConsumeOkMethodFrame() {
            var message = new BasicConsumeOk(
                consumerTag: Random.Word()
            );
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as BasicConsumeOk;

                Assert.Equal(expected: message.ConsumerTag, actual: unwrappedMessage.ConsumerTag);
            }
            else {
                // No `BasicConsumeOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsBasicCancelOkMethodFrame() {
            var message = new BasicCancelOk(
                consumerTag: Random.Word()
            );
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as BasicCancelOk;

                Assert.Equal(expected: message.ConsumerTag, actual: unwrappedMessage.ConsumerTag);
            }
            else {
                // No `BasicCancelOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsBasicReturnMethodFrame() {
            var message = new BasicReturn(
                replyCode   : Random.UShort(),
                replyText   : Random.Word(),
                exchangeName: Random.Word(),
                routingKey  : Random.Word()
            );
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as BasicReturn;

                Assert.Equal(expected: message.ExchangeName, actual: unwrappedMessage.ExchangeName);
                Assert.Equal(expected: message.ReplyCode, actual: unwrappedMessage.ReplyCode);
                Assert.Equal(expected: message.ReplyText, actual: unwrappedMessage.ReplyText);
                Assert.Equal(expected: message.RoutingKey, actual: unwrappedMessage.RoutingKey);
            }
            else {
                // No `BasicReturn` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsBasicDeliverMethodFrame() {
            var message = new BasicDeliver(
                consumerTag : Random.Word(),
                deliveryTag : Random.ULong(),
                redelivered : Random.Bool(),
                exchangeName: Random.Word()
            );
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as BasicDeliver;

                Assert.Equal(expected: message.ConsumerTag, actual: unwrappedMessage.ConsumerTag);
                Assert.Equal(expected: message.DeliveryTag, actual: unwrappedMessage.DeliveryTag);
                Assert.Equal(expected: message.ExchangeName, actual: unwrappedMessage.ExchangeName);
                Assert.Equal(expected: message.Redelivered, actual: unwrappedMessage.Redelivered);
            }
            else {
                // No `BasicDeliver` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsBasicGetOkMethodFrame() {
            var message = new BasicGetOk(
                deliveryTag : Random.ULong(),
                redelivered : Random.Bool(),
                exchangeName: Random.Word(),
                routingKey  : Random.Word(),
                messageCount: Random.UInt()
            );
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, message));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                var unwrappedMessage = _unwrappedMessage as BasicGetOk;

                Assert.Equal(expected: message.DeliveryTag, actual: unwrappedMessage.DeliveryTag);
                Assert.Equal(expected: message.ExchangeName, actual: unwrappedMessage.ExchangeName);
                Assert.Equal(expected: message.MessageCount, actual: unwrappedMessage.MessageCount);
                Assert.Equal(expected: message.Redelivered, actual: unwrappedMessage.Redelivered);
                Assert.Equal(expected: message.RoutingKey, actual: unwrappedMessage.RoutingKey);
            }
            else {
                // No `BasicGetOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsBasicGetEmptyMethodFrame() {
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, new BasicGetEmpty()));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `BasicGetEmpty` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsBasicRecoverOkMethodFrame() {
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, new BasicRecoverOk()));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `BasicRecoverOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsTransactionSelectOkMethodFrame() {
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, new TransactionSelectOk()));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `TransactionSelectOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsTransactionCommitOkMethodFrame() {
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, new TransactionCommitOk()));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `TransactionCommitOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }

        [Fact]
        public void UnwrapsTransactionRollbackOkMethodFrame() {
            _context.Send(_subject, RawFrame.Wrap(_channelNumber, new TransactionRollbackOk()));

            if (_messageReceivedSignal.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.True(_messageReceivedSignal.IsSet);
            }
            else {
                // No `TransactionRollbackOk` command was received within 100 millis...
                throw new TimeoutException("Timeout occurred before command was received");
            }
        }
    }
}
