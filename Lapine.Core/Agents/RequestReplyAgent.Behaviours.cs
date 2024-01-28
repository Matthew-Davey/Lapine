namespace Lapine.Agents;

using System.Runtime.ExceptionServices;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

static partial class RequestReplyAgent<TRequest, TReply> where TRequest : ICommand where TReply : ICommand {
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
}
