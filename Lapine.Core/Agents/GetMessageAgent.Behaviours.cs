namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

static partial class GetMessageAgent {
    static Behaviour<Protocol> AwaitingGetMessages(IObservable<RawFrame> receivedFrames, IDispatcherAgent dispatcher, CancellationToken cancellationToken) =>
        async context => {
            switch (context.Message) {
                case GetMessage(var queue, var acknowledgements, var replyChannel): {
                    var subscription = receivedFrames.Subscribe(onNext: frame => {
                        context.Self.PostAsync(new FrameReceived(RawFrame.Unwrap(frame)));
                    });

                    await dispatcher.Dispatch(new BasicGet(
                        QueueName: queue,
                        NoAck: acknowledgements switch {
                            Acknowledgements.Auto   => true,
                            Acknowledgements.Manual => false,
                            _                       => false
                        }
                    ));

                    var cancelTimeout = cancellationToken.Register(() => context.Self.PostAsync(new Timeout()));

                    return context with {
                        Behaviour = AwaitingBasicGetOkOrEmpty(subscription, cancelTimeout, replyChannel)
                    };
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingGetMessages)}' behaviour.");
            }
        };

    static Behaviour<Protocol> AwaitingBasicGetOkOrEmpty(IDisposable subscription, CancellationTokenRegistration cancelTimeout, AsyncReplyChannel replyChannel) =>
        async context => {
            switch (context.Message) {
                case FrameReceived(BasicGetEmpty): {
                    await cancelTimeout.DisposeAsync();
                    replyChannel.Reply(new NoMessage());
                    await context.Self.StopAsync();
                    subscription.Dispose();
                    return context;
                }
                case FrameReceived(BasicGetOk ok): {
                    var deliveryInfo = DeliveryInfo.FromBasicGetOk(ok);
                    return context with {
                        Behaviour = AwaitingContentHeader(subscription, cancelTimeout, deliveryInfo, replyChannel)
                    };
                }
                case Timeout: {
                    replyChannel.Reply(new TimeoutException());
                    await context.Self.StopAsync();
                    subscription.Dispose();
                    return context;
                }
                case FrameReceived(ChannelClose close): {
                    await cancelTimeout.DisposeAsync();
                    replyChannel.Reply(AmqpException.Create(close.ReplyCode, close.ReplyText));
                    await context.Self.StopAsync();
                    subscription.Dispose();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingBasicGetOkOrEmpty)}' behaviour.");
            }
        };

    static Behaviour<Protocol> AwaitingContentHeader(IDisposable subscription, CancellationTokenRegistration cancelTimeout, DeliveryInfo deliveryInfo, AsyncReplyChannel replyChannel) =>
        async context => {
            switch (context.Message) {
                case FrameReceived(ContentHeader { BodySize: 0 } header): {
                    await cancelTimeout.DisposeAsync();
                    replyChannel.Reply(new Message(deliveryInfo, header.Properties, Memory<Byte>.Empty));
                    await context.Self.StopAsync();
                    subscription.Dispose();
                    return context;
                }
                case FrameReceived(ContentHeader { BodySize: > 0 } header): {
                    return context with {
                        Behaviour = AwaitingContentBody(subscription, cancelTimeout, deliveryInfo, header, replyChannel)
                    };
                }
                case FrameReceived(TimeoutException timeout): {
                    replyChannel.Reply(timeout);
                    await context.Self.StopAsync();
                    subscription.Dispose();
                    return context;
                }
                case FrameReceived(ChannelClose close): {
                    await cancelTimeout.DisposeAsync();
                    replyChannel.Reply(AmqpException.Create(close.ReplyCode, close.ReplyText));
                    await context.Self.StopAsync();
                    subscription.Dispose();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingContentHeader)}' behaviour.");
            }
        };

    static Behaviour<Protocol> AwaitingContentBody(IDisposable subscription, CancellationTokenRegistration cancelTimeout, DeliveryInfo deliveryInfo, ContentHeader header, AsyncReplyChannel replyChannel) {
        var buffer = new MemoryBufferWriter<Byte>((Int32)header.BodySize);

        return async context => {
            switch (context.Message) {
                case FrameReceived(ReadOnlyMemory<Byte> segment): {
                    buffer.WriteBytes(segment.Span);
                    if ((UInt64)buffer.WrittenCount >= header.BodySize) {
                        await cancelTimeout.DisposeAsync();
                        replyChannel.Reply(new Message(deliveryInfo, header.Properties, buffer.WrittenMemory));
                        await context.Self.StopAsync();
                        subscription.Dispose();
                    }
                    return context;
                }
                case Timeout: {
                    replyChannel.Reply(new TimeoutException());
                    await context.Self.StopAsync();
                    subscription.Dispose();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingContentBody)}' behaviour.");
            }
        };
    }
}
