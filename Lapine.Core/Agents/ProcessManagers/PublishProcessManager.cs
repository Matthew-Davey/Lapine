namespace Lapine.Agents.ProcessManagers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Lapine.Agents.Middleware;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;
using Proto;
using Proto.Timers;

using static System.Threading.Tasks.Task;
using static Lapine.Agents.DispatcherAgent.Protocol;
using static Lapine.Agents.SocketAgent.Protocol;

class PublishProcessManager : IActor {
    readonly Behavior _behaviour;
    readonly UInt16 _channelId;
    readonly PID _dispatcher;
    readonly String _exchange;
    readonly String _routingKey;
    readonly RoutingFlags _routingFlags;
    readonly (BasicProperties Properties, ReadOnlyMemory<Byte> Body) _message;
    readonly UInt64 _maxFrameSize;
    readonly Boolean _publisherConfirmsEnabled;
    readonly UInt64 _deliveryTag;
    readonly TimeSpan _timeout;
    readonly TaskCompletionSource _promise;

    public PublishProcessManager(UInt16 channelId, PID dispatcher, String exchange, String routingKey, RoutingFlags routingFlags, (BasicProperties Properties, ReadOnlyMemory<Byte> Body) message, UInt64 maxFrameSize, Boolean publisherConfirmsEnabled, UInt64 deliveryTag, TimeSpan timeout, TaskCompletionSource promise) {
        _behaviour                = new Behavior(Unstarted);
        _channelId                = channelId;
        _dispatcher               = dispatcher;
        _exchange                 = exchange;
        _routingKey               = routingKey;
        _routingFlags             = routingFlags;
        _message                  = message;
        _maxFrameSize             = maxFrameSize;
        _publisherConfirmsEnabled = publisherConfirmsEnabled;
        _deliveryTag              = deliveryTag;
        _timeout                  = timeout;
        _promise                  = promise;
    }

    static public Props Create(UInt16 channelId, PID dispatcher, String exchange, String routingKey, RoutingFlags routingFlags, (BasicProperties Properties, ReadOnlyMemory<Byte> Body) message, UInt64 maxFrameSize, Boolean publisherConfirmsEnabled, UInt64 deliveryTag, TimeSpan timeout, TaskCompletionSource promise) =>
        Props.FromProducer(() => new PublishProcessManager(channelId, dispatcher, exchange, routingKey, routingFlags, message, maxFrameSize, publisherConfirmsEnabled, deliveryTag, timeout, promise))
            .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames());

    public Task ReceiveAsync(IContext context) =>
        _behaviour.ReceiveAsync(context);

    Task Unstarted(IContext context) {
        switch (context.Message) {
            case Started: {
                var subscription = context.System.EventStream.Subscribe<FrameReceived>(
                    predicate: message => message.Frame.Channel == _channelId,
                    action   : message => context.Send(context.Self!, message)
                );
                context.Send(_dispatcher, Dispatch.Command(new BasicPublish(
                    ExchangeName: _exchange,
                    RoutingKey  : _routingKey,
                    Mandatory   : _routingFlags.HasFlag(RoutingFlags.Mandatory),
                    Immediate   : _routingFlags.HasFlag(RoutingFlags.Immediate)
                )));
                context.Send(_dispatcher, Dispatch.ContentHeader(new ContentHeader(
                    ClassId   : 0x3C,
                    BodySize  : (UInt64)_message.Body.Length,
                    Properties: _message.Properties
                )));
                foreach (var segment in _message.Body.Split((Int32)_maxFrameSize)) {
                    context.Send(_dispatcher, Dispatch.ContentBody(segment));
                }
                if (_publisherConfirmsEnabled) {
                    var scheduledTimeout = context.Scheduler().SendOnce(_timeout, context.Self!, new TimeoutException());
                    _behaviour.Become(AwaitingPublisherConfirm(subscription, scheduledTimeout));
                }
                else {
                    _promise.SetResult();
                    _behaviour.Become(Done(subscription));
                    context.Stop(context.Self!);
                }
                break;
            }
        }
        return CompletedTask;
    }

    Receive AwaitingPublisherConfirm(EventStreamSubscription<Object> subscription, CancellationTokenSource scheduledTimeout) =>
        (IContext context) => {
            switch (context.Message) {
                case BasicAck ack when ack.DeliveryTag == _deliveryTag: {
                    scheduledTimeout.Cancel();
                    _promise.SetResult();
                    _behaviour.Become(Done(subscription));
                    context.Stop(context.Self!);
                    break;
                }
                case BasicNack nack when nack.DeliveryTag == _deliveryTag: {
                    scheduledTimeout.Cancel();
                    _promise.SetException(new AmqpException("Server rejected the message")); // Why?
                    _behaviour.Become(Done(subscription));
                    context.Stop(context.Self!);
                    break;
                }
                case TimeoutException timeout: {
                    _promise.SetException(timeout);
                    _behaviour.Become(Done(subscription));
                    context.Stop(context.Self!);
                    break;
                }
                case ChannelClose close: {
                    scheduledTimeout.Cancel();
                    _promise.SetException(AmqpException.Create(close.ReplyCode, close.ReplyText));
                    _behaviour.Become(Done(subscription));
                    context.Stop(context.Self!);
                    break;
                }
            }
            return CompletedTask;
        };

    static Receive Done(EventStreamSubscription<Object> subscription) =>
        (IContext context) => {
            switch (context.Message) {
                case Stopping: {
                    context.System.EventStream.Unsubscribe(subscription);
                    break;
                }
            }
            return CompletedTask;
        };
}
