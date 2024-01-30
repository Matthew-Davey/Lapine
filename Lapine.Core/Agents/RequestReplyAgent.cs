namespace Lapine.Agents;

using Lapine.Protocol;
using Lapine.Protocol.Commands;

interface IRequestReplyAgent<in TRequest, TReply>
where TRequest : ICommand
where TReply : ICommand {
    Task<TReply> Request(TRequest request);
}

static partial class RequestReplyAgent<TRequest, TReply> where TRequest : ICommand where TReply : ICommand {
    static public IRequestReplyAgent<TRequest, TReply> StartNew(IObservable<RawFrame> receivedFrames, IDispatcherAgent dispatcher, CancellationToken cancellationToken = default) =>
        new Wrapper(Agent<Protocol>.StartNew(AwaitingRequest(receivedFrames, dispatcher, cancellationToken)));
}
