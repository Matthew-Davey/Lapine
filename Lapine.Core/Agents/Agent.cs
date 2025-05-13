namespace Lapine.Agents;

using System.Threading.Channels;

interface IAgent<in TProtocol> {
    ValueTask PostAsync(TProtocol message, CancellationToken cancellationToken = default);
    ValueTask PostAndReplyAsync(Func<AsyncReplyChannel, TProtocol> messageFactory);
    ValueTask<TReply> PostAndReplyAsync<TReply>(Func<AsyncReplyChannel<TReply>, TProtocol> messageFactory);
    ValueTask StopAsync();
}

class AsyncReplyChannel(TaskCompletionSource promise) {
    public void Complete() => promise.SetResult();
    public void Fault(Exception fault) => promise.SetException(fault);
}

class AsyncReplyChannel<TReply>(TaskCompletionSource<TReply> promise) {
    public void Reply(TReply response) => promise.SetResult(response);
    public void Fault(Exception fault) => promise.SetException(fault);
}

class Agent<TProtocol> : IAgent<TProtocol> {
    readonly Channel<TProtocol> _mailbox;
    readonly Task _messageLoop;

    Agent(Channel<TProtocol> mailbox, Behaviour<TProtocol> initialBehaviour) {
        _mailbox = mailbox;
        _messageLoop = Task.Factory.StartNew(async () => {
            var context = new MessageContext<TProtocol>(this, initialBehaviour, default!);

            while (await _mailbox.Reader.WaitToReadAsync()) {
                var message = await _mailbox.Reader.ReadAsync();
                context = await context.Behaviour(context with { Message = message });
            }
        });
    }

    static public IAgent<TProtocol> StartNew(Behaviour<TProtocol> initialBehaviour) {
        var mailbox = Channel.CreateUnbounded<TProtocol>(new UnboundedChannelOptions {
            SingleReader = true
        });
        return new Agent<TProtocol>(mailbox, initialBehaviour);
    }

    public async ValueTask PostAsync(TProtocol message, CancellationToken cancellationToken = default) =>
        await _mailbox.Writer.WriteAsync(message, cancellationToken);

    public async ValueTask PostAndReplyAsync(Func<AsyncReplyChannel, TProtocol> messageFactory) {
        ArgumentNullException.ThrowIfNull(messageFactory);

        var promise = new TaskCompletionSource();
        var replyChannel = new AsyncReplyChannel(promise);
        var message = messageFactory(replyChannel);

        await _mailbox.Writer.WriteAsync(message);

        await promise.Task;
    }

    public async ValueTask<TReply> PostAndReplyAsync<TReply>(Func<AsyncReplyChannel<TReply>, TProtocol> messageFactory) {
        ArgumentNullException.ThrowIfNull(messageFactory);

        var promise = new TaskCompletionSource<TReply>();
        var replyChannel = new AsyncReplyChannel<TReply>(promise);
        var message = messageFactory(replyChannel);

        await _mailbox.Writer.WriteAsync(message);

        return await promise.Task;
    }

    public async ValueTask StopAsync() {
        _mailbox.Writer.Complete();
        await _messageLoop;
    }
}
