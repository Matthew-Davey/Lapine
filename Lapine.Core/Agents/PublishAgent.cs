namespace Lapine.Agents;

using System.Reactive.Linq;
using System.Runtime.ExceptionServices;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

interface IPublishAgent {
    Task<Result<Boolean>> Publish(String exchange, String routingKey, RoutingFlags routingFlags, (BasicProperties Properties, ReadOnlyMemory<Byte> Body) message);
}

class PublishAgent : IPublishAgent {
    readonly IAgent<Protocol> _agent;

    PublishAgent(IAgent<Protocol> agent) =>
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));

    abstract record Protocol;
    record PublishMessage(String Exchange, String RoutingKey, RoutingFlags RoutingFlags, (BasicProperties Properties, ReadOnlyMemory<Byte> Body) Message, AsyncReplyChannel ReplyChannel) : Protocol;
    record Timeout : Protocol;
    record FrameReceived(ICommand Command) : Protocol;

    static public IPublishAgent Create(IObservable<RawFrame> receivedFrames, IDispatcherAgent dispatcher, UInt64 maxFrameSize, Boolean publisherConfirmsEnabled, UInt64 deliveryTag, CancellationToken cancellationToken) =>
        new PublishAgent(Agent<Protocol>.StartNew(Unstarted(receivedFrames, dispatcher, maxFrameSize, publisherConfirmsEnabled, deliveryTag, cancellationToken)));

    static Behaviour<Protocol> Unstarted(IObservable<RawFrame> receivedFrames, IDispatcherAgent dispatcher, UInt64 maxFrameSize, Boolean publisherConfirmsEnabled, UInt64 deliveryTag, CancellationToken cancellationToken) =>
        async context => {
            switch (context.Message) {
                case PublishMessage(var exchange, var routingKey, var routingFlags, var message, var replyChannel): {
                    var frameSubscription = receivedFrames
                        .Where(frame => frame.Type == FrameType.Method)
                        .Subscribe(frame => context.Self.PostAsync(new FrameReceived(RawFrame.UnwrapMethod(frame))));
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
                        var cancelTimeout = cancellationToken.Register(() => context.Self.PostAsync(new Timeout()));
                        return context with {
                            Behaviour = AwaitingPublisherConfirm(deliveryTag, frameSubscription, replyChannel, cancelTimeout)
                        };
                    }
                    else {
                        replyChannel.Reply(new Result<Boolean>.Ok(true));
                        frameSubscription.Dispose();
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
                    replyChannel.Reply(new Result<Boolean>.Ok(true));
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                case FrameReceived(BasicNack nack) when nack.DeliveryTag == deliveryTag: {
                    await scheduledTimeout.DisposeAsync();
                    replyChannel.Reply(new Result<Boolean>.Fault(ExceptionDispatchInfo.Capture(new AmqpException("Server rejected the message")))); // Why?
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                case Timeout: {
                    replyChannel.Reply(new Result<Boolean>.Fault(ExceptionDispatchInfo.Capture(new TimeoutException())));
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                case FrameReceived(ChannelClose close): {
                    await scheduledTimeout.DisposeAsync();
                    replyChannel.Reply(new Result<Boolean>.Fault(ExceptionDispatchInfo.Capture(AmqpException.Create(close.ReplyCode, close.ReplyText))));
                    frameSubscription.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingPublisherConfirm)}' behaviour.");
            }
        };

    async Task<Result<Boolean>> IPublishAgent.Publish(String exchange, String routingKey, RoutingFlags routingFlags, (BasicProperties Properties, ReadOnlyMemory<Byte> Body) message) {
        var reply = await _agent.PostAndReplyAsync(replyChannel => new PublishMessage(exchange, routingKey, routingFlags, message, replyChannel));
        return (Result<Boolean>) reply;
    }
}
