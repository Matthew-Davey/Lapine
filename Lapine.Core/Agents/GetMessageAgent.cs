namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

using static Lapine.Agents.DispatcherAgent.Protocol;
using static Lapine.Agents.GetMessageAgent.Protocol;

static class GetMessageAgent {
    static public class Protocol {
        public record GetMessages(String Queue, Acknowledgements Acknowledgements);
        public record NoMessages;
    }

    static public IAgent Create(IObservable<RawFrame> receivedFrames, IAgent dispatcher, CancellationToken cancellationToken) =>
        Agent.StartNew(AwaitingGetMessages(receivedFrames, dispatcher, cancellationToken));

    static Behaviour AwaitingGetMessages(IObservable<RawFrame> receivedFrames, IAgent dispatcher, CancellationToken cancellationToken) =>
        async context => {
            switch (context.Message) {
                case (GetMessages(var queue, var acknowledgements), AsyncReplyChannel replyChannel): {
                    var subscription = receivedFrames.Subscribe(onNext: frame => {
                        if (frame.Type == FrameType.Method)
                            context.Self.PostAsync(RawFrame.UnwrapMethod(frame));
                        if (frame.Type == FrameType.Header)
                            context.Self.PostAsync(RawFrame.UnwrapContentHeader(frame));
                        if (frame.Type == FrameType.Body)
                            context.Self.PostAsync(RawFrame.UnwrapContentBody(frame));
                    });

                    await dispatcher.PostAsync(Dispatch.Command(new BasicGet(
                        QueueName: queue,
                        NoAck: acknowledgements switch {
                            Acknowledgements.Auto   => true,
                            Acknowledgements.Manual => false,
                            _                       => false
                        }
                    )));

                    var cancelTimeout = cancellationToken.Register(() => context.Self.PostAsync(new TimeoutException()));

                    return context with {
                        Behaviour = AwaitingBasicGetOkOrEmpty(subscription, cancelTimeout, replyChannel)
                    };
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingGetMessages)}' behaviour.");
            }
        };

    static Behaviour AwaitingBasicGetOkOrEmpty(IDisposable subscription, CancellationTokenRegistration cancelTimeout, AsyncReplyChannel replyChannel) =>
        async context => {
            switch (context.Message) {
                case BasicGetEmpty: {
                    await cancelTimeout.DisposeAsync();
                    replyChannel.Reply(new NoMessages());
                    context.Self.StopAsync();
                    subscription.Dispose();
                    return context;
                }
                case BasicGetOk ok: {
                    var deliveryInfo = DeliveryInfo.FromBasicGetOk(ok);
                    return context with {
                        Behaviour = AwaitingContentHeader(subscription, cancelTimeout, deliveryInfo, replyChannel)
                    };
                }
                case TimeoutException timeout: {
                    replyChannel.Reply(timeout);
                    context.Self.StopAsync();
                    subscription.Dispose();
                    return context;
                }
                case ChannelClose close: {
                    await cancelTimeout.DisposeAsync();
                    replyChannel.Reply(AmqpException.Create(close.ReplyCode, close.ReplyText));
                    context.Self.StopAsync();
                    subscription.Dispose();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingBasicGetOkOrEmpty)}' behaviour.");
            }
        };

    static Behaviour AwaitingContentHeader(IDisposable subscription, CancellationTokenRegistration cancelTimeout, DeliveryInfo deliveryInfo, AsyncReplyChannel replyChannel) =>
        async context => {
            switch (context.Message) {
                case ContentHeader { BodySize: 0 } header: {
                    await cancelTimeout.DisposeAsync();
                    replyChannel.Reply((deliveryInfo, header.Properties, Memory<Byte>.Empty));
                    context.Self.StopAsync();
                    subscription.Dispose();
                    return context;
                }
                case ContentHeader { BodySize: > 0 } header: {
                    return context with {
                        Behaviour = AwaitingContentBody(subscription, cancelTimeout, deliveryInfo, header, replyChannel)
                    };
                }
                case TimeoutException timeout: {
                    replyChannel.Reply(timeout);
                    context.Self.StopAsync();
                    subscription.Dispose();
                    return context;
                }
                case ChannelClose close: {
                    await cancelTimeout.DisposeAsync();
                    replyChannel.Reply(AmqpException.Create(close.ReplyCode, close.ReplyText));
                    context.Self.StopAsync();
                    subscription.Dispose();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingContentHeader)}' behaviour.");
            }
        };

    static Behaviour AwaitingContentBody(IDisposable subscription, CancellationTokenRegistration cancelTimeout, DeliveryInfo deliveryInfo, ContentHeader header, AsyncReplyChannel replyChannel) {
        var buffer = new MemoryBufferWriter<Byte>((Int32)header.BodySize);

        return async context => {
            switch (context.Message) {
                case ReadOnlyMemory<Byte> segment: {
                    buffer.WriteBytes(segment.Span);
                    if ((UInt64)buffer.WrittenCount >= header.BodySize) {
                        await cancelTimeout.DisposeAsync();
                        replyChannel.Reply((deliveryInfo, header.Properties, buffer.WrittenMemory));
                        context.Self.StopAsync();
                        subscription.Dispose();
                    }
                    return context;
                }
                case TimeoutException timeout: {
                    replyChannel.Reply(timeout);
                    context.Self.StopAsync();
                    subscription.Dispose();
                    return context;
                }
                case Stopped: {
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingContentBody)}' behaviour.");
            }
        };
    }
}
