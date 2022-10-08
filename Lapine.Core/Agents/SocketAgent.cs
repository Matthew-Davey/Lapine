namespace Lapine.Agents;

using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks.Dataflow;
using Lapine.Client;
using Lapine.Protocol;

using static SocketAgent.Messages;

class SocketAgent {
    static public class Messages {
        public record struct Connect(IPEndPoint EndPoint, TimeSpan ConnectTimeout);
        public record struct SocketConnected(IAsyncResult AsyncResult);
        public record struct ConnectionFailed(Exception Fault);
        public record struct ConnectionEstablished;
        public record struct Disconnected(Exception Fault);
        public record struct Transmit(ISerializable Payload);
        public record struct Poll;
        public record struct PollComplete(IAsyncResult AsyncResult);
        public record struct Timeout;
        public record struct FrameReceived(Frame Frame);
    }

    readonly ActionBlock<Object> _inbox;
    readonly BroadcastBlock<Object> _outbox;

    public SocketAgent() {
        var context = new Context(Message: null, Behaviour: Disconnected());

        _inbox = new ActionBlock<Object>(message => {
            Console.WriteLine($"SocketAgent <- {message}");

            context = context.Behaviour(context with { Message = message });
        }, new ExecutionDataflowBlockOptions {
            BoundedCapacity        = 100,
            EnsureOrdered          = true,
            MaxDegreeOfParallelism = 1,
        });
        _outbox = new BroadcastBlock<Object>(x => x);
        _outbox.LinkTo(new ActionBlock<Object>(message => Console.WriteLine($"SocketAgent -> {message}")));
    }

    public void Post(Object message) =>
        _inbox.Post(message);

    public ISourceBlock<Object> Outbox =>
        _outbox;

    Behaviour Disconnected() => context => {
        switch (context.Message) {
            case Connect(var endpoint, var timeout): {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.BeginConnect(
                    remoteEP: endpoint,
                    callback: asyncResult => _inbox.Post(new SocketConnected(asyncResult)),
                    state   : null
                );
                var cancelTimeout = _inbox.DelayPost(new Timeout(), timeout);
                return context with { Behaviour = Connecting(socket, cancelTimeout) };
            }
            default:
                return context;
        }
    };

    Behaviour Connecting(Socket socket, CancellationTokenSource cancelTimeout) => context => {
        switch (context.Message) {
            case SocketConnected(var asyncResult): {
                try {
                    socket.EndConnect(asyncResult);
                    cancelTimeout.Cancel();
                    _outbox.Post(new ConnectionEstablished());
                    return context with { Behaviour = Connected(socket) };
                }
                catch (SocketException fault) {
                    _outbox.Post(new ConnectionFailed(fault));
                    return context with { Behaviour = Disconnected() };
                }
            }
            case Timeout: {
                var timeoutException = new TimeoutException();
                _outbox.Post(new ConnectionFailed(timeoutException));
                return context with { Behaviour = Disconnected() };
            }
            default:
                return context;
        }
    };

    Behaviour Connected(Socket socket) {
        var (frameBuffer, tail) = (new Byte[ConnectionConfiguration.DefaultMaximumFrameSize], 0);

        _inbox.Post(new Poll());

        return context => {
            switch (context.Message) {
                case Poll: {
                    socket.BeginReceive(
                        buffer     : frameBuffer,
                        offset     : tail,
                        size       : 1024,
                        socketFlags: SocketFlags.None,
                        callback   : asyncResult => _inbox.Post(new PollComplete(asyncResult)),
                        state      : null
                    );

                    return context;
                }
                case PollComplete(var asyncResult): {
                    tail += socket.EndReceive(asyncResult);

                    if (tail > 0) {
                        var buffer = new ReadOnlyMemory<Byte>(frameBuffer, 0, tail);
                        while (Frame.Deserialize(ref buffer, out var frame)) {
                            _outbox.Post(new FrameReceived(frame));
                        }
                        buffer.CopyTo(frameBuffer);
                    }

                    _inbox.Post(new Poll());

                    return context;
                }
                case Transmit(var payload): {
                    try {
                        var writer = new MemoryBufferWriter<Byte>();
                        payload.Serialize(writer);

                        socket.Send(writer.WrittenSpan);
                        return context;
                    }
                    catch (SocketException fault) {
                        _outbox.Post(new Disconnected(fault));
                        return context with { Behaviour = Disconnected() };
                    }
                }
                default:
                    return context;
            }
        };
    }
}
