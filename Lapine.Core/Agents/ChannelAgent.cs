namespace Lapine.Agents;

using System.Threading.Tasks.Dataflow;

class ChannelAgent {
    static public class Messages {
        public readonly record struct Start(UInt32 Channel);
    }

    readonly ActionBlock<Object> _inbox;
    readonly BroadcastBlock<Object> _outbox;

    public ChannelAgent() {
        var context = new Context(Message: null, Behaviour: Unstarted);

        _inbox = new ActionBlock<Object>(message => {
            Console.WriteLine($"ChannelAgent <- {message}");

            context = context.Behaviour(context with { Message = message });
        }, new ExecutionDataflowBlockOptions {
            BoundedCapacity        = 100,
            EnsureOrdered          = true,
            MaxDegreeOfParallelism = 1
        });
        _outbox = new BroadcastBlock<Object>(x => x, new DataflowBlockOptions {
            EnsureOrdered = true
        });
    }

    public void Post(Object message) =>
        _inbox.Post(message);

    public ISourceBlock<Object> Outbox =>
        _outbox;

    Behaviour Unstarted => context => {
        switch (context.Message) {
            case Messages.Start(var channel): {
                _outbox.LinkTo(new ActionBlock<Object>(message => Console.WriteLine($"ChannelAgent-{channel} -> {message}")));
                return context with { Behaviour = Ready(channel)};
            }
        }
        return context;
    };

    Behaviour Ready(UInt32 channel) => context => {
        return context;
    };
}
