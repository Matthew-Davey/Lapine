namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

interface IGetMessageAgent {
    Task<GetMessageAgent.GetMessageResult> GetMessages(String queue, Acknowledgements acknowledgements);
}

class GetMessageAgent : IGetMessageAgent {
    readonly IAgent<Protocol> _agent;

    GetMessageAgent(IAgent<Protocol> agent) =>
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));

    public abstract record GetMessageResult;
    public record NoMessage : GetMessageResult;
    public record Message(DeliveryInfo DeliveryInfo, BasicProperties Properties, ReadOnlyMemory<Byte> Body) : GetMessageResult;

    abstract record Protocol;
    record GetMessage(String Queue, Acknowledgements Acknowledgements, AsyncReplyChannel ReplyChannel) : Protocol;
    record FrameReceived(Object Frame) : Protocol;
    record Timeout : Protocol;

    static public IGetMessageAgent Create(IObservable<RawFrame> receivedFrames, IDispatcherAgent dispatcher, CancellationToken cancellationToken) =>
        new GetMessageAgent(Agent<Protocol>.StartNew(AwaitingGetMessages(receivedFrames, dispatcher, cancellationToken)));

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

    async Task<GetMessageResult> IGetMessageAgent.GetMessages(String queue, Acknowledgements acknowledgements) {
        switch (await _agent.PostAndReplyAsync(replyChannel => new GetMessage(queue, acknowledgements, replyChannel))) {
            case GetMessageResult reply:
                return reply;
            case Exception fault:
                throw fault;
            default:
                throw new Exception("Unexpected return value");
        }
    }
}
