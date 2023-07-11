using System.Reactive;

namespace Lapine.Agents;

using System.Reactive.Linq;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

using static Lapine.Agents.ConsumerAgent.Protocol;
using static Lapine.Agents.MessageAssemblerAgent.Protocol;

static class MessageAssemblerAgent {
    static public class Protocol {
        public record Begin(IObservable<RawFrame> Frames, IAgent Parent);
    }

    static public IAgent StartNew() =>
        Agent.StartNew(Unstarted());

    static Behaviour Unstarted() =>
        async context => {
            switch (context.Message) {
                case Begin(var frames, var parent): {
                    var frameSubscription = frames
                        .Select(frame => RawFrame.Unwrap(frame))
                        .Subscribe(message => context.Self.PostAsync(message));

                    return context with {Behaviour = AwaitingBasicDeliver(parent, frameSubscription)};
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Unstarted)}' behaviour.");
            }
        };

    static Behaviour AwaitingBasicDeliver(IAgent listener, IDisposable frameSubscription) =>
        async context => {
            switch (context.Message) {
                case BasicDeliver deliver: {
                    return context with { Behaviour = AwaitingContentHeader(listener, frameSubscription, DeliveryInfo.FromBasicDeliver(deliver)) };
                }
                case Stopped: {
                    frameSubscription.Dispose();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingBasicDeliver)}' behaviour.");
            }
        };

    static Behaviour AwaitingContentHeader(IAgent listener, IDisposable frameSubscription, DeliveryInfo deliveryInfo) =>
        async context => {
            switch (context.Message) {
                case ContentHeader { BodySize: 0 } header: {
                    await listener.PostAsync(new ConsumeMessage(
                        Delivery  : deliveryInfo,
                        Properties: header.Properties,
                        Buffer    : new MemoryBufferWriter<Byte>()
                    ));
                    return context with { Behaviour = AwaitingBasicDeliver(listener, frameSubscription) };
                }
                case ContentHeader header: {
                    return context with { Behaviour = AwaitingContentBody(listener, frameSubscription, deliveryInfo, header) };
                }
                case Stopped: {
                    frameSubscription.Dispose();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingContentHeader)}' behaviour.");
            }
        };

    static Behaviour AwaitingContentBody(IAgent listener, IDisposable frameSubscription, DeliveryInfo deliveryInfo, ContentHeader header) {
        var writer = new MemoryBufferWriter<Byte>((Int32)header.BodySize);

        return async context => {
            switch (context.Message) {
                case ReadOnlyMemory<Byte> segment: {
                    writer.WriteBytes(segment.Span);
                    if ((UInt64)writer.WrittenCount >= header.BodySize) {
                        await listener.PostAsync(new ConsumeMessage(
                            Delivery  : deliveryInfo,
                            Properties: header.Properties,
                            Buffer    : writer
                        ));
                        return context with { Behaviour = AwaitingBasicDeliver(listener, frameSubscription) };
                    }
                    return context;
                }
                case Stopped: {
                    frameSubscription.Dispose();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingContentBody)}' behaviour.");
            }
        };
    }
}
