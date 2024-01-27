namespace Lapine.Agents;

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

interface IMessageAssemblerAgent {
    Task<IObservable<(DeliveryInfo DeliveryInfo, BasicProperties Properties, MemoryBufferWriter<Byte> Buffer)>> Begin(IObservable<RawFrame> frameStream, IConsumerAgent parent);
    Task Stop();
}

class MessageAssemblerAgent : IMessageAssemblerAgent {
    readonly IAgent<Protocol> _agent;

    MessageAssemblerAgent(IAgent<Protocol> agent) =>
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));

    abstract record Protocol;
    record Begin(IObservable<RawFrame> Frames, AsyncReplyChannel ReplyChannel) : Protocol;
    record Stop : Protocol;
    record FrameReceived(Object Frame) : Protocol;

    static public IMessageAssemblerAgent StartNew() =>
        new MessageAssemblerAgent(Agent<Protocol>.StartNew(Unstarted()));

    static Behaviour<Protocol> Unstarted() =>
        async context => {
            switch (context.Message) {
                case Begin(var frames, var parent): {
                    var frameSubscription = frames
                        .Select(frame => RawFrame.Unwrap(frame))
                        .Subscribe(message => context.Self.PostAsync(new FrameReceived(message)));

                    var receivedMessages = new Subject<(DeliveryInfo, BasicProperties, MemoryBufferWriter<Byte>)>();

                    return context with { Behaviour = AwaitingBasicDeliver(receivedMessages, frameSubscription) };
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Unstarted)}' behaviour.");
            }
        };

    static Behaviour<Protocol> AwaitingBasicDeliver(Subject<(DeliveryInfo, BasicProperties, MemoryBufferWriter<Byte>)> receivedMessages, IDisposable frameSubscription) =>
        async context => {
            switch (context.Message) {
                case FrameReceived(BasicDeliver deliver): {
                    return context with { Behaviour = AwaitingContentHeader(receivedMessages, frameSubscription, DeliveryInfo.FromBasicDeliver(deliver)) };
                }
                case Stop: {
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingBasicDeliver)}' behaviour.");
            }
        };

    static Behaviour<Protocol> AwaitingContentHeader(Subject<(DeliveryInfo, BasicProperties, MemoryBufferWriter<Byte>)> receivedMessages, IDisposable frameSubscription, DeliveryInfo deliveryInfo) =>
        async context => {
            switch (context.Message) {
                case FrameReceived(ContentHeader { BodySize: 0 } header): {
                    receivedMessages.OnNext((deliveryInfo, header.Properties, new MemoryBufferWriter<Byte>()));
                    return context with { Behaviour = AwaitingBasicDeliver(receivedMessages, frameSubscription) };
                }
                case FrameReceived(ContentHeader header): {
                    return context with { Behaviour = AwaitingContentBody(receivedMessages, frameSubscription, deliveryInfo, header) };
                }
                case Stop: {
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingContentHeader)}' behaviour.");
            }
        };

    static Behaviour<Protocol> AwaitingContentBody(Subject<(DeliveryInfo, BasicProperties, MemoryBufferWriter<Byte>)> receivedMessages, IDisposable frameSubscription, DeliveryInfo deliveryInfo, ContentHeader header) {
        var writer = new MemoryBufferWriter<Byte>((Int32)header.BodySize);

        return async context => {
            switch (context.Message) {
                case FrameReceived(ReadOnlyMemory<Byte> segment): {
                    writer.WriteBytes(segment.Span);
                    if ((UInt64)writer.WrittenCount >= header.BodySize) {
                        receivedMessages.OnNext((deliveryInfo, header.Properties, writer));
                        return context with { Behaviour = AwaitingBasicDeliver(receivedMessages, frameSubscription) };
                    }
                    return context;
                }
                case Stop: {
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingContentBody)}' behaviour.");
            }
        };
    }

    async Task<IObservable<(DeliveryInfo DeliveryInfo, BasicProperties Properties, MemoryBufferWriter<Byte> Buffer)>> IMessageAssemblerAgent.Begin(IObservable<RawFrame> frameStream, IConsumerAgent parent) {
        var reply = await _agent.PostAndReplyAsync(replyChannel => new Begin(frameStream, replyChannel));
        return (IObservable<(DeliveryInfo DeliveryInfo, BasicProperties Properties, MemoryBufferWriter<Byte> Buffer)>) reply;
    }

    async Task IMessageAssemblerAgent.Stop() =>
        await _agent.PostAsync(new Stop());
}
