namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

using static Lapine.Agents.DispatcherAgent.Protocol;

static class RequestReplyAgent {
    static public IAgent StartNew<TRequest, TReply>(IObservable<RawFrame> receivedFrames, IAgent dispatcher, CancellationToken cancellationToken = default)
        where TRequest : ICommand
        where TReply : ICommand {
        return Agent.StartNew(AwaitingRequest<TRequest, TReply>(receivedFrames, dispatcher, cancellationToken));
    }

    static Behaviour AwaitingRequest<TRequest, TReply>(IObservable<RawFrame> receivedFrames, IAgent dispatcher, CancellationToken cancellationToken)
        where TRequest : ICommand
        where TReply : ICommand =>
        async context => {
            switch (context.Message) {
                case Started: {
                    return context;
                }
                case (TRequest request, AsyncReplyChannel replyChannel): {
                    var framesSubscription = receivedFrames
                        .Subscribe(frame => context.Self.PostAsync(RawFrame.UnwrapMethod(frame)));

                    var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.Token.Register(() => context.Self.PostAsync(new TimeoutException()));

                    await dispatcher.PostAsync(Dispatch.Command(request));

                    return context with { Behaviour = AwaitingReply<TReply>(framesSubscription, cts, replyChannel) };
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingRequest)}' behaviour.");
            }
        };

    static Behaviour AwaitingReply<TReply>(IDisposable framesSubscription, IDisposable scheduledTimeout, AsyncReplyChannel replyChannel)
        where TReply : ICommand =>
        async context => {
            switch (context.Message) {
                case TReply reply: {
                    replyChannel.Reply(reply);
                    context.Self.StopAsync();
                    return context;
                }
                case TimeoutException timeout: {
                    replyChannel.Reply(timeout);
                    context.Self.StopAsync();
                    return context;
                }
                case ChannelClose(var replyCode, var replyText, _): {
                    replyChannel.Reply(AmqpException.Create(replyCode, replyText));
                    context.Self.StopAsync();
                    return context;
                }
                case Stopped: {
                    framesSubscription.Dispose();
                    scheduledTimeout.Dispose();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingReply)}' behaviour.");
            }
        };
}
