namespace Lapine.Agents.ProcessManagers;

using Lapine.Agents.Middleware;
using Lapine.Client;
using Lapine.Protocol.Commands;
using Proto;
using Proto.Timers;

using static System.Threading.Tasks.Task;
using static Lapine.Agents.DispatcherAgent.Protocol;
using static Lapine.Agents.SocketAgent.Protocol;

class RequestReplyProcessManager<TRequest, TReply> : IActor
    where TRequest : ICommand
    where TReply : ICommand {
    readonly Behavior _behaviour;
    readonly UInt16 _channelId;
    readonly PID _dispatcher;
    readonly TRequest _request;
    readonly TimeSpan _timeout;
    readonly TaskCompletionSource _promise;

    public RequestReplyProcessManager(UInt16 channelId, PID dispatcher, TRequest request, TimeSpan timeout, TaskCompletionSource promise) {
        _behaviour  = new Behavior(Unstarted);
        _channelId  = channelId;
        _dispatcher = dispatcher;
        _request    = request;
        _timeout    = timeout;
        _promise    = promise;
    }

    static public Props Create(UInt16 channelId, PID dispatcher, TRequest request, TimeSpan timeout, TaskCompletionSource promise) =>
        Props.FromProducer(() => new RequestReplyProcessManager<TRequest, TReply>(channelId, dispatcher, request, timeout, promise))
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
                context.Send(_dispatcher, Dispatch.Command(_request));
                var scheduledTimeout = context.Scheduler().SendOnce(_timeout, context.Self!, new TimeoutException());
                _behaviour.Become(AwaitingReply(subscription, scheduledTimeout));
                break;
            }
        }
        return CompletedTask;
    }

    Receive AwaitingReply(EventStreamSubscription<Object> subscription, CancellationTokenSource scheduledTimeout) =>
        context => {
            switch (context.Message) {
                case TReply: {
                    scheduledTimeout.Cancel();
                    _promise.SetResult();
                    context.Stop(context.Self!);
                    break;
                }
                case TimeoutException timeout: {
                    _promise.SetException(timeout);
                    context.Stop(context.Self!);
                    break;
                }
                case ChannelClose close: {
                    scheduledTimeout.Cancel();
                    _promise.SetException(AmqpException.Create(close.ReplyCode, close.ReplyText));
                    context.Stop(context.Self!);
                    break;
                }
                case Stopping: {
                    subscription!.Unsubscribe();
                    break;
                }
            }
            return CompletedTask;
        };
}
