using System.Reactive.Linq;

namespace Lapine.Agents;

using System.Runtime.ExceptionServices;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

interface IRequestReplyAgent<in TRequest, TReply>
where TRequest : ICommand
where TReply : ICommand {
    Task<Result<TReply>> Request(TRequest request);
}

class RequestReplyAgent<TRequest, TReply> : IRequestReplyAgent<TRequest, TReply>
where TRequest : ICommand
where TReply : ICommand {
    readonly IAgent<Protocol> _agent;

    RequestReplyAgent(IAgent<Protocol> agent) =>
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));

    abstract record Protocol;
    record SendRequest(TRequest Request, AsyncReplyChannel ReplyChannel) : Protocol;
    record OnFrameReceived(ICommand Command) : Protocol;
    record OnTimeout : Protocol;

    static public IRequestReplyAgent<TRequest, TReply> StartNew(IObservable<RawFrame> receivedFrames, IDispatcherAgent dispatcher, CancellationToken cancellationToken = default) =>
        new RequestReplyAgent<TRequest, TReply>(Agent<Protocol>.StartNew(AwaitingRequest(receivedFrames, dispatcher, cancellationToken)));

    static Behaviour<Protocol> AwaitingRequest(IObservable<RawFrame> receivedFrames, IDispatcherAgent dispatcher, CancellationToken cancellationToken) =>
        async context => {
            switch (context.Message) {
                case SendRequest(var request, var replyChannel): {
                    var framesSubscription = receivedFrames
                        .Subscribe(frame => context.Self.PostAsync(new OnFrameReceived(RawFrame.UnwrapMethod(frame))));

                    var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.Token.Register(() => context.Self.PostAsync(new OnTimeout()));

                    await dispatcher.Dispatch(request);

                    return context with { Behaviour = AwaitingReply(framesSubscription, cts, replyChannel) };
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingRequest)}' behaviour.");
            }
        };

    static Behaviour<Protocol> AwaitingReply(IDisposable framesSubscription, IDisposable scheduledTimeout, AsyncReplyChannel replyChannel) =>
        async context => {
            switch (context.Message) {
                case OnFrameReceived(TReply reply): {
                    replyChannel.Reply(new Result<TReply>.Ok(reply));
                    framesSubscription.Dispose();
                    scheduledTimeout.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                case OnTimeout: {
                    replyChannel.Reply(new Result<TReply>.Fault(ExceptionDispatchInfo.Capture(new TimeoutException())));
                    framesSubscription.Dispose();
                    scheduledTimeout.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                case OnFrameReceived(ChannelClose(var replyCode, var replyText, _)): {
                    replyChannel.Reply(new Result<TReply>.Fault(ExceptionDispatchInfo.Capture(AmqpException.Create(replyCode, replyText))));
                    framesSubscription.Dispose();
                    scheduledTimeout.Dispose();
                    await context.Self.StopAsync();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingReply)}' behaviour.");
            }
        };

    async Task<Result<TReply>> IRequestReplyAgent<TRequest, TReply>.Request(TRequest request) {
        var reply = (Result<TReply>) await _agent.PostAndReplyAsync(replyChannel => new SendRequest(request, replyChannel));
        return reply;
    }
}
