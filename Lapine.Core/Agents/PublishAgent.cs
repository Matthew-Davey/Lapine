using System.Reactive.Linq;

namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

using static Lapine.Agents.DispatcherAgent.Protocol;
using static Lapine.Agents.PublishAgent.Protocol;

static class PublishAgent {
    static public class Protocol {
        public record PublishMessage(String Exchange, String RoutingKey, RoutingFlags RoutingFlags, (BasicProperties Properties, ReadOnlyMemory<Byte> Body) Message);
    }

    static public IAgent Create(IObservable<RawFrame> receivedFrames, IAgent dispatcher, UInt64 maxFrameSize, Boolean publisherConfirmsEnabled, UInt64 deliveryTag, CancellationToken cancellationToken) =>
        Agent.StartNew(Unstarted(receivedFrames, dispatcher, maxFrameSize, publisherConfirmsEnabled, deliveryTag, cancellationToken));

    static Behaviour Unstarted(IObservable<RawFrame> receivedFrames, IAgent dispatcher, UInt64 maxFrameSize, Boolean publisherConfirmsEnabled, UInt64 deliveryTag, CancellationToken cancellationToken) =>
        async context => {
            switch (context.Message) {
                case (PublishMessage(var exchange, var routingKey, var routingFlags, var message), AsyncReplyChannel replyChannel): {
                    var frameSubscription = receivedFrames
                        .Where(frame => frame.Type == FrameType.Method)
                        .Subscribe(frame => context.Self.PostAsync(RawFrame.UnwrapMethod(frame)));
                    await dispatcher.PostAsync(Dispatch.Command(new BasicPublish(
                        ExchangeName: exchange,
                        RoutingKey  : routingKey,
                        Mandatory   : routingFlags.HasFlag(RoutingFlags.Mandatory),
                        Immediate   : routingFlags.HasFlag(RoutingFlags.Immediate)
                    )));
                    await dispatcher.PostAsync(Dispatch.ContentHeader(new ContentHeader(
                        ClassId   : 0x3C,
                        BodySize  : (UInt64) message.Body.Length,
                        Properties: message.Properties
                    )));
                    foreach (var segment in message.Body.Split((Int32) maxFrameSize)) {
                        await dispatcher.PostAsync(Dispatch.ContentBody(segment));
                    }

                    if (publisherConfirmsEnabled) {
                        var cancelTimeout = cancellationToken.Register(() => context.Self.PostAsync(new TimeoutException()));
                        return context with {
                            Behaviour = AwaitingPublisherConfirm(deliveryTag, frameSubscription, replyChannel, cancelTimeout)
                        };
                    }
                    else {
                        replyChannel.Reply(true);
                        frameSubscription.Dispose();
                        await context.Self.StopAsync();
                        return context;
                    }

                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Unstarted)}' behaviour.");
            }
        };

    static Behaviour AwaitingPublisherConfirm(UInt64 deliveryTag, IDisposable frameSubscription, AsyncReplyChannel replyChannel, CancellationTokenRegistration scheduledTimeout) =>
        async context => {
            switch (context.Message) {
                case BasicAck ack when ack.DeliveryTag == deliveryTag: {
                    await scheduledTimeout.DisposeAsync();
                    replyChannel.Reply(true);
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                case BasicNack nack when nack.DeliveryTag == deliveryTag: {
                    await scheduledTimeout.DisposeAsync();
                    replyChannel.Reply(new AmqpException("Server rejected the message")); // Why?
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                case TimeoutException timeout: {
                    replyChannel.Reply(timeout);
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                case ChannelClose close: {
                    await scheduledTimeout.DisposeAsync();
                    replyChannel.Reply(AmqpException.Create(close.ReplyCode, close.ReplyText));
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingPublisherConfirm)}' behaviour.");
            }
        };
}
