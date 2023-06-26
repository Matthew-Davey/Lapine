namespace Lapine.Agents;

using System.Runtime.CompilerServices;
using System.Threading.Channels;

interface IAgent {
    ValueTask PostAsync(Object message, CancellationToken cancellationToken = default);
    ValueTask<Object> PostAndReplyAsync(Object message);
    ValueTask StopAsync();
}

class AsyncReplyChannel {
    readonly Action<Object> _reply;

    public AsyncReplyChannel(Action<Object> reply) =>
        _reply = reply ?? throw new ArgumentNullException(nameof(reply));

    public void Reply(Object response) => _reply(response);
}

public record Started;
public record Stopped;

class Agent : IAgent {
    readonly Channel<Object> _mailbox;
    readonly Task _messageLoop;

    Agent(Channel<Object> mailbox, Behaviour initialBehaviour) {
        _mailbox = mailbox;
        _messageLoop = Task.Factory.StartNew(async () => {
            var context = new MessageContext(this, initialBehaviour, new Started());

            context = await context.Behaviour(context);

            while (await _mailbox.Reader.WaitToReadAsync()) {
                var message = await _mailbox.Reader.ReadAsync();
                context = await context.Behaviour(context with { Message = message });
            }

            await context.Behaviour(context with { Message = new Stopped() });
        });
    }

    static public IAgent StartNew(Behaviour initialBehaviour) {
        var mailbox = Channel.CreateUnbounded<Object>(new UnboundedChannelOptions {
            SingleReader = true
        });
        return new Agent(mailbox, initialBehaviour);
    }

    public async ValueTask PostAsync(Object message, CancellationToken cancellationToken = default) =>
        await _mailbox.Writer.WriteAsync(message, cancellationToken);

    public async ValueTask<Object> PostAndReplyAsync(Object message) {
        var promise      = AsyncValueTaskMethodBuilder<Object>.Create();
        var replyChannel = new AsyncReplyChannel(reply => promise.SetResult(reply));

        await _mailbox.Writer.WriteAsync((message, replyChannel));

        return await promise.Task;
    }

    public async ValueTask StopAsync() {
        _mailbox.Writer.Complete();
        await _messageLoop;
    }
}
