namespace Lapine.Agents;

using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Threading.Tasks.Dataflow;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

using static System.Text.Encoding;
using static SocketAgent2.Protocol;

class HandshakeAgent {
    readonly ConnectionConfiguration _connectionConfiguration;
    readonly SocketAgent2 _socketAgent;
    readonly BufferBlock<Object> _inbox;
    readonly ActionBlock<Object> _messageHandler;
    readonly BufferBlock<Object> _outbox;

    public HandshakeAgent(ConnectionConfiguration connectionConfiguration, SocketAgent2 socketAgent) {
        _connectionConfiguration = connectionConfiguration;
        _socketAgent = socketAgent ?? throw new ArgumentNullException(nameof(socketAgent));

        var context = new Context(Message: null, Behaviour: AwaitingConnection());

        _inbox = new BufferBlock<Object>(new DataflowBlockOptions {
            EnsureOrdered = true
        });
        _messageHandler = new ActionBlock<Object>(message => {
            context = context.Behaviour(context with { Message = message });
        }, new ExecutionDataflowBlockOptions {
            MaxDegreeOfParallelism = 1
        });
        _outbox = new BufferBlock<Object>(new DataflowBlockOptions {
            EnsureOrdered = true
        });

        _inbox.LinkTo(_messageHandler);

        var subscription = _socketAgent.Events.Subscribe(_inbox.AsObserver());
    }

    public IObservable<Object> Events =>
        _outbox.AsObservable();

    Behaviour AwaitingConnection() => context => {
        switch (context.Message) {
            case Connected connected: {
                _socketAgent.Transmit(ProtocolHeader.Default);
                return context with { Behaviour = AwaitingConnectionStart() };
            }
        }
        return context;
    };

    Behaviour AwaitingConnectionStart() => context => {
        switch (context.Message) {
            case FrameReceived { Frame: MethodFrame { Channel: var channel, Command: ConnectionStart connectionStart } }: {
                var authenticationResponse = _connectionConfiguration.AuthenticationStrategy.Respond(
                    stage    : 0,
                    challenge: Span<Byte>.Empty
                );
                var command = new ConnectionStartOk {
                    PeerProperties = _connectionConfiguration.PeerProperties.ToDictionary(),
                    Mechanism      = _connectionConfiguration.AuthenticationStrategy.Mechanism,
                    Response       = UTF8.GetString(authenticationResponse),
                    Locale         = _connectionConfiguration.Locale
                };
                var frame = new MethodFrame(channel, command.CommandId, command);
                _socketAgent.Transmit(frame);
                return context with { Behaviour = AwaitingConnectionSecureOrTune() };
                break;
            }
        }
        return context;
    };

    Behaviour AwaitingConnectionSecureOrTune() => context => {
        return context;
    };

    Behaviour AwaitingConnectionOpenOk() => context => {
        return context;
    };
}

class SocketAgent2 {
    static public class Protocol {
        public record struct Connect(IPEndPoint EndPoint, TimeSpan ConnectTimeout, IObserver<Object> Observer);
        public record struct Connected(IAsyncResult AsyncResult);
        public record struct Transmit(ISerializable payload);
        public record struct Poll;
        public record struct PollComplete(IAsyncResult AsyncResult);
        public record struct Timeout;
        public record struct FrameReceived(Frame Frame);
    }

    readonly BufferBlock<Object> _inbox;
    readonly ActionBlock<Object> _messageHandler;
    readonly BufferBlock<Object> _outbox;

    public SocketAgent2() {
        var context = new Context(Message: null, Behaviour: Disconnected());

        _inbox = new BufferBlock<Object>(new DataflowBlockOptions {
            BoundedCapacity = 100,
            EnsureOrdered   = true
        });
        _messageHandler = new ActionBlock<Object>(message => {
            context = context.Behaviour(context with { Message = message });
        }, new ExecutionDataflowBlockOptions {
            MaxDegreeOfParallelism = 1
        });
        _outbox = new BufferBlock<Object>(new DataflowBlockOptions {
            EnsureOrdered = true
        });

        _inbox.LinkTo(_messageHandler);
    }

    public void Post(Object message) =>
        _inbox.Post(message);

    public IObservable<Object> Events =>
        _outbox.AsObservable();

    public async ValueTask Connect(IPEndPoint endpoint, TimeSpan timeout) {
        var taskCompletionSource = new TaskCompletionSource();
        using var observer = new AnonymousObserver<Object>(
            onNext     : _ => { },
            onError    : taskCompletionSource.SetException,
            onCompleted: taskCompletionSource.SetResult
        );
        _inbox.Post(new Connect(endpoint, timeout, observer));
        await taskCompletionSource.Task;
    }

    public void Transmit(ISerializable payload) =>
        _inbox.Post(new Transmit(payload));

    Behaviour Disconnected() => context => {
        switch (context.Message) {
            case Connect(var endpoint, var timeout, var observer): {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.BeginConnect(
                    remoteEP: endpoint,
                    callback: asyncResult => _inbox.Post(new Connected(asyncResult)),
                    state   : null
                );
                var cancelTimeout = _inbox.DelayPost(new Timeout(), timeout);
                return context with { Behaviour = Connecting(socket, cancelTimeout, observer) };
            }
            default:
                return context;
        }
    };

    Behaviour Connecting(Socket socket, CancellationTokenSource cancelTimeout, IObserver<Object> observer) => context => {
        switch (context.Message) {
            case Connected(var asyncResult): {
                try {
                    socket.EndConnect(asyncResult);
                    cancelTimeout.Cancel();
                    observer.OnCompleted();
                    return context with { Behaviour = Connected(socket) };
                }
                catch (SocketException fault) {
                    observer.OnError(fault);
                    return context with { Behaviour = Faulted(fault) };
                }
            }
            case Timeout: {
                var timeoutException = new TimeoutException();
                observer.OnError(timeoutException);
                return context with { Behaviour = Faulted(timeoutException) };
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
                        while (Frame.Deserialize(frameBuffer.AsSpan(0, tail), out var frame, out var remaining)) {
                            remaining.CopyTo(frameBuffer);
                            tail = remaining.Length;
                            _outbox.Post(new FrameReceived(frame));
                        }
                    }

                    _inbox.Post(new Poll());

                    return context;
                }
                case Transmit(var payload): {
                    try {
                        var writer = new ArrayBufferWriter<Byte>();
                        payload.Serialize(writer);

                        socket.Send(writer.WrittenSpan);
                        return context;
                    }
                    catch (SocketException fault) {
                        return context with { Behaviour = Faulted(fault) };
                    }
                }
                default:
                    return context;
            }
        };
    }

    Behaviour Faulted(Exception fault) => context => {
        return context;
    };
}
