namespace Lapine.Agents;

using System.Runtime.CompilerServices;
using System.Threading.Channels;

interface IAgent<in TProtocol> {
    ValueTask PostAsync(TProtocol message, CancellationToken cancellationToken = default);
    ValueTask<Object> PostAndReplyAsync(Func<AsyncReplyChannel, TProtocol> messageFactory);
    ValueTask StopAsync();
}

class AsyncReplyChannel(Action<Object> reply) {
    public void Reply(Object response) => reply(response);
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

    public async ValueTask<Object> PostAndReplyAsync(Func<AsyncReplyChannel, TProtocol> messageFactory) {
        ArgumentNullException.ThrowIfNull(messageFactory);

        var promise = AsyncValueTaskMethodBuilder<Object>.Create();
        var replyChannel = new AsyncReplyChannel(reply => promise.SetResult(reply));
        var message = messageFactory(replyChannel);

        await _mailbox.Writer.WriteAsync(message);

        return await promise.Task;
    }

    public async ValueTask StopAsync() {
        _mailbox.Writer.Complete();
        await _messageLoop;
    }
}
