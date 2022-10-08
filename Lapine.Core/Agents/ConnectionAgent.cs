namespace Lapine.Agents;

using System.Collections.Immutable;
using System.Net;
using System.Threading.Tasks.Dataflow;
using Lapine.Client;
using Lapine.Protocol;

class ConnectionAgent {
    static public class Messages {
        public record struct Connect(ConnectionConfiguration ConnectionConfiguration);
        public record struct ConnectionFailed(AggregateException Failures);
    }

    readonly ActionBlock<Object> _inbox;
    readonly BroadcastBlock<Object> _outbox;

    record ConnectionState(
        ConnectionConfiguration ConnectionConfiguration,
        SocketAgent Socket,
        IImmutableDictionary<UInt32, ChannelAgent> Channels
    );

    public ConnectionAgent() {
        var state = new ConnectionState(
            ConnectionConfiguration: ConnectionConfiguration.Default,
            Socket: new SocketAgent(),
            Channels: ImmutableDictionary<UInt32, ChannelAgent>.Empty
        );
        var context = new Context<ConnectionState>(Message: null, Behaviour: Disconnected, State: state);

        _inbox = new ActionBlock<Object>(message => {
            Console.WriteLine($"ConnectionAgent <- {message}");
            context = context.Behaviour(context with { Message = message });
        }, new ExecutionDataflowBlockOptions {
            BoundedCapacity        = 100,
            EnsureOrdered          = true,
            MaxDegreeOfParallelism = 1
        });
        _outbox = new BroadcastBlock<Object>(x => x, new DataflowBlockOptions {
            EnsureOrdered = true
        });
        _outbox.LinkTo(new ActionBlock<Object>(message => Console.WriteLine($"ConnectionAgent -> {message}")));
    }

    public void Post(Object message) =>
        _inbox.Post(message);

    public ISourceBlock<Object> Outbox =>
        _outbox;

    Behaviour<ConnectionState> Disconnected => context => {
        switch (context.Message) {
            case Messages.Connect(var connectionConfiguration): {
                var endpoints = connectionConfiguration.GetConnectionSequence();

                context.State.Socket.Outbox.LinkTo(_inbox);
                context.State.Socket.Post(new SocketAgent.Messages.Connect(endpoints[0], connectionConfiguration.ConnectionTimeout));

                return context with {
                    Behaviour = Connecting(ImmutableList.Create(endpoints[1..]), ImmutableList<Exception>.Empty),
                    State     = context.State with { ConnectionConfiguration = connectionConfiguration }
                };
            }
        }
        return context;
    };

    Behaviour<ConnectionState> Connecting(ImmutableList<IPEndPoint> remainingEndpoints, ImmutableList<Exception> accumulatedFailures) => context => {
        switch (context.Message) {
            // The socket connected, begin connection negotiation phase...
            case SocketAgent.Messages.ConnectionEstablished: {
                var channel0 = new ChannelAgent();
                channel0.Post(new ChannelAgent.Messages.Start(Channel: 0));
                channel0.Outbox.LinkTo(_inbox);
                // TODO: Start handshake agent...
                context.State.Socket.Post(new SocketAgent.Messages.Transmit(ProtocolHeader.Default));
                return context with {
                    Behaviour = Negotiating(remainingEndpoints, accumulatedFailures),
                    State     = context.State with { Channels = context.State.Channels.Add(0, channel0) }
                };
            }
            // The socket failed to connect, attempt to connect to the next endpoint...
            case SocketAgent.Messages.ConnectionFailed { Fault: var fault } when remainingEndpoints.Any(): {
                context.State.Socket.Post(new SocketAgent.Messages.Connect(remainingEndpoints[0], context.State.ConnectionConfiguration.ConnectionTimeout));
                return context with { Behaviour = Connecting(remainingEndpoints.RemoveAt(0), accumulatedFailures.Add(fault)) };
            }
            // The socket failed to connect and no further endpoints are available...
            case SocketAgent.Messages.ConnectionFailed { Fault: var fault } when !remainingEndpoints.Any(): {
                _outbox.Post(new Messages.ConnectionFailed(new AggregateException(accumulatedFailures.Add(fault))));
                return context with { Behaviour = Disconnected };
            }
        }
        return context;
    };

    Behaviour<ConnectionState> Negotiating(ImmutableList<IPEndPoint> remainingEndpoints, ImmutableList<Exception> accumulatedFailures) => context => {
        switch (context.Message) {
            case SocketAgent.Messages.FrameReceived(var frame): {
                // todo: route frame to channel...
                return context;
            }
            // The socket faulted during negotiation, attempt to connect to the next endpoint...
            case SocketAgent.Messages.Disconnected(var fault) when remainingEndpoints.Any(): {
                context.State.Socket.Post(new SocketAgent.Messages.Transmit(ProtocolHeader.Default));
                return context with {
                    Behaviour = Connecting(remainingEndpoints.RemoveAt(0), accumulatedFailures.Add(fault))
                };
            }
            // TODO: socket faulted during negotiation and no further endpoints available...
        }
        return context;
    };
}
