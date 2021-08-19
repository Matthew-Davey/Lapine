namespace Lapine.Agents.ProcessManagers {
    using System;
    using System.Threading.Tasks;
    using Lapine.Agents.Middleware;
    using Lapine.Client;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.ConsumerAgent.Protocol;
    using static Lapine.Agents.SocketAgent.Protocol;

    static class MessageAssemblerAgent {
        static public Props Create(UInt16 channelId, PID listener) =>
            Props.FromProducer(() => new Actor(channelId, listener))
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames())
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundContentHeaderFrames())
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundContentBodyFrames());

        record State(
            EventStreamSubscription<Object> Subscription
        );

        class Actor : IActor {
            readonly Behavior _behaviour;
            readonly UInt16 _channelId;
            readonly PID _listener;

            public Actor(UInt16 channelId, PID listener) {
                _behaviour = new Behavior(Unstarted);
                _channelId = channelId;
                _listener  = listener;
            }

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Unstarted(IContext context) {
                switch (context.Message) {
                    case Started: {
                        var subscription = context.System.EventStream.Subscribe<FrameReceived>(
                            predicate: message => message.Frame.Channel == _channelId,
                            action   : message => context.Send(context.Self!, message)
                        );
                        _behaviour.Become(AwaitingBasicDeliver(new State(subscription)));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive AwaitingBasicDeliver(State state) =>
                (IContext context) => {
                    switch (context.Message) {
                        case BasicDeliver deliver: {
                            _behaviour.Become(AwaitingContentHeader(state, DeliveryInfo.FromBasicDeliver(deliver)));
                            break;
                        }
                        case Stopping: {
                            context.System.EventStream.Unsubscribe(state.Subscription);
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingContentHeader(State state, DeliveryInfo deliveryInfo) =>
                (IContext context) => {
                    switch (context.Message) {
                        case ContentHeader { BodySize: 0 } header: {
                            context.Send(_listener, new ConsumeMessage(
                                Delivery: deliveryInfo,
                                Properties: header.Properties,
                                Buffer: new MemoryBufferWriter<Byte>()
                            ));
                            _behaviour.Become(AwaitingBasicDeliver(state));
                            break;
                        }
                        case ContentHeader header: {
                            _behaviour.Become(AwaitingContentBody(state, deliveryInfo, header));
                            break;
                        }
                        case Stopping: {
                            context.System.EventStream.Unsubscribe(state.Subscription);
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingContentBody(State state, DeliveryInfo deliveryInfo, ContentHeader header) {
                var buffer = new MemoryBufferWriter<Byte>((Int32)header.BodySize);

                return (IContext context) => {
                    switch (context.Message) {
                        case ReadOnlyMemory<Byte> segment: {
                            buffer.WriteBytes(segment.Span);
                            if ((UInt64)buffer.WrittenCount >= header.BodySize) {
                                context.Send(_listener, new ConsumeMessage(
                                    Delivery: deliveryInfo,
                                    Properties: header.Properties,
                                    Buffer: buffer
                                ));
                                _behaviour.Become(AwaitingBasicDeliver(state));
                            }
                            break;
                        }
                        case Stopping: {
                            context.System.EventStream.Unsubscribe(state.Subscription);
                            break;
                        }
                    }
                    return CompletedTask;
                };
            }
        }
    }
}
