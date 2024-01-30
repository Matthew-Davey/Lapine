namespace Lapine.Agents;

using System.Reactive.Linq;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

static partial class PublishAgent {
    static Behaviour<Protocol> Unstarted(IObservable<RawFrame> receivedFrames, IDispatcherAgent dispatcher, UInt64 maxFrameSize, Boolean publisherConfirmsEnabled, UInt64 deliveryTag, CancellationToken cancellationToken) =>
        async context => {
            switch (context.Message) {
                case PublishMessage(var exchange, var routingKey, var routingFlags, var message, var replyChannel): {
                    await dispatcher.Dispatch(new BasicPublish(
                        ExchangeName: exchange,
                        RoutingKey  : routingKey,
                        Mandatory   : routingFlags.HasFlag(RoutingFlags.Mandatory),
                        Immediate   : routingFlags.HasFlag(RoutingFlags.Immediate)
                    ));
                    await dispatcher.Dispatch(new ContentHeader(
                        ClassId   : 0x3C,
                        BodySize  : (UInt64) message.Body.Length,
                        Properties: message.Properties
                    ));
                    foreach (var segment in message.Body.Split((Int32) maxFrameSize)) {
                        await dispatcher.Dispatch(segment);
                    }

                    if (publisherConfirmsEnabled) {
                        var frameSubscription = receivedFrames
                            .Where(frame => frame.Type == FrameType.Method)
                            .Subscribe(frame => context.Self.PostAsync(new FrameReceived(RawFrame.UnwrapMethod(frame))));

                        var cancelTimeout = cancellationToken.Register(() => context.Self.PostAsync(new Timeout()));
                        return context with {
                            Behaviour = AwaitingPublisherConfirm(deliveryTag, frameSubscription, replyChannel, cancelTimeout)
                        };
                    }
                    else {
                        replyChannel.Complete();
                        await context.Self.StopAsync();
                        return context;
                    }

                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Unstarted)}' behaviour.");
            }
        };

    static Behaviour<Protocol> AwaitingPublisherConfirm(UInt64 deliveryTag, IDisposable frameSubscription, AsyncReplyChannel replyChannel, CancellationTokenRegistration scheduledTimeout) =>
        async context => {
            switch (context.Message) {
                case FrameReceived(BasicAck ack) when ack.DeliveryTag == deliveryTag: {
                    await scheduledTimeout.DisposeAsync();
                    replyChannel.Complete();
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                case FrameReceived(BasicNack nack) when nack.DeliveryTag == deliveryTag: {
                    await scheduledTimeout.DisposeAsync();
                    replyChannel.Fault(new AmqpException("Server rejected the message")); // Why?
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                case Timeout: {
                    replyChannel.Fault(new TimeoutException());
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                case FrameReceived(ChannelClose close): {
                    await scheduledTimeout.DisposeAsync();
                    replyChannel.Fault(AmqpException.Create(close.ReplyCode, close.ReplyText));
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingPublisherConfirm)}' behaviour.");
            }
        };
}
