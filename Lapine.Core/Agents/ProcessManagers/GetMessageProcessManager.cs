namespace Lapine.Agents.ProcessManagers;

using Lapine.Agents.Middleware;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;
using Proto;
using Proto.Timers;

using static System.Threading.Tasks.Task;
using static Lapine.Agents.DispatcherAgent.Protocol;
using static Lapine.Agents.SocketAgent.Protocol;

class GetMessageProcessManager : IActor {
    readonly Behavior _behaviour;
    readonly UInt16 _channelId;
    readonly PID _dispatcher;
    readonly String _queue;
    readonly Acknowledgements _acknowledgements;
    readonly TimeSpan _timeout;
    readonly TaskCompletionSource<(DeliveryInfo, BasicProperties, ReadOnlyMemory<Byte>)?> _promise;

    public GetMessageProcessManager(UInt16 channelId, PID dispatcher, String queue, Acknowledgements acknowledgements, TimeSpan timeout, TaskCompletionSource<(DeliveryInfo, BasicProperties, ReadOnlyMemory<Byte>)?> promise) {
        _behaviour        = new Behavior(Unstarted);
        _channelId        = channelId;
        _dispatcher       = dispatcher;
        _queue            = queue;
        _acknowledgements = acknowledgements;
        _timeout          = timeout;
        _promise          = promise;
    }

    static public Props Create(UInt16 channelId, PID dispatcher, String queue, Acknowledgements acknowledgements, TimeSpan timeout, TaskCompletionSource<(DeliveryInfo, BasicProperties, ReadOnlyMemory<Byte>)?> promise) =>
        Props.FromProducer(() => new GetMessageProcessManager(channelId, dispatcher, queue, acknowledgements, timeout, promise))
            .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames())
            .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundContentHeaderFrames())
            .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundContentBodyFrames());

    public Task ReceiveAsync(IContext context) =>
        _behaviour.ReceiveAsync(context);

    Task Unstarted(IContext context) {
        switch (context.Message) {
            case Started: {
                var subscription = context.System.EventStream.Subscribe<FrameReceived>(
                    predicate: message => message.Frame.Channel == _channelId,
                    action   : message => context.Send(context.Self!, message)
                );
                context.Send(_dispatcher, Dispatch.Command(new BasicGet(
                    QueueName: _queue,
                    NoAck    : _acknowledgements switch {
                        Acknowledgements.Auto   => true,
                        Acknowledgements.Manual => false,
                        _ => false
                    }
                )));
                var scheduledTimeout = context.Scheduler().SendOnce(
                    delay  : _timeout,
                    target : context.Self!,
                    message: new TimeoutException()
                );
                _behaviour.Become(AwaitingBasicGetOKOrEmpty(subscription, scheduledTimeout));
                break;
            }
        }
        return CompletedTask;
    }

    Receive AwaitingBasicGetOKOrEmpty(EventStreamSubscription<Object> subscription, CancellationTokenSource scheduledTimeout) =>
        (IContext context) => {
            switch (context.Message) {
                case BasicGetEmpty: {
                    scheduledTimeout.Cancel();
                    _promise.SetResult(null);
                    context.Stop(context.Self!);
                    _behaviour.Become(Done(subscription));
                    break;
                }
                case BasicGetOk ok: {
                    var deliveryInfo = DeliveryInfo.FromBasicGetOk(ok);
                    _behaviour.Become(AwaitingContentHeader(subscription, scheduledTimeout, deliveryInfo));
                    break;
                }
                case TimeoutException timeout: {
                    _promise.SetException(timeout);
                    context.Stop(context.Self!);
                    _behaviour.Become(Done(subscription));
                    break;
                }
                case ChannelClose close: {
                    scheduledTimeout.Cancel();
                    _promise.SetException(AmqpException.Create(close.ReplyCode, close.ReplyText));
                    context.Stop(context.Self!);
                    _behaviour.Become(Done(subscription));
                    break;
                }
            }
            return CompletedTask;
        };

    Receive AwaitingContentHeader(EventStreamSubscription<Object> subscription, CancellationTokenSource scheduledTimeout, DeliveryInfo deliveryInfo) =>
        (IContext context) => {
            switch (context.Message) {
                case ContentHeader { BodySize: 0 } header: {
                    scheduledTimeout.Cancel();
                    _promise.SetResult((deliveryInfo, header.Properties, Memory<Byte>.Empty));
                    context.Stop(context.Self!);
                    _behaviour.Become(Done(subscription));
                    break;
                }
                case ContentHeader { BodySize: > 0 } header: {
                    _behaviour.Become(AwaitingContentBody(subscription, scheduledTimeout, deliveryInfo, header));
                    break;
                }
                case TimeoutException timeout: {
                    _promise.SetException(timeout);
                    context.Stop(context.Self!);
                    _behaviour.Become(Done(subscription));
                    break;
                }
                case ChannelClose close: {
                    scheduledTimeout.Cancel();
                    _promise.SetException(AmqpException.Create(close.ReplyCode, close.ReplyText));
                    context.Stop(context.Self!);
                    _behaviour.Become(Done(subscription));
                    break;
                }
            }
            return CompletedTask;
        };

    Receive AwaitingContentBody(EventStreamSubscription<Object> subscription, CancellationTokenSource scheduledTimeout, DeliveryInfo deliveryInfo, ContentHeader header) {
        var buffer = new MemoryBufferWriter<Byte>((Int32)header.BodySize);

        return (IContext context) => {
            switch (context.Message) {
                case ReadOnlyMemory<Byte> segment: {
                    buffer.WriteBytes(segment.Span);
                    if ((UInt64)buffer.WrittenCount >= header.BodySize) {
                        scheduledTimeout.Cancel();
                        _promise.SetResult((deliveryInfo, header.Properties, buffer.WrittenMemory));
                        context.Stop(context.Self!);
                        _behaviour.Become(Done(subscription));
                    }
                    break;
                }
                case TimeoutException timeout: {
                    _promise.SetException(timeout);
                    context.Stop(context.Self!);
                    _behaviour.Become(Done(subscription));
                    break;
                }
            }
            return CompletedTask;
        };
    }

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
