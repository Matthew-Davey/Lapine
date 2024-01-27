namespace Lapine.Agents;

using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using Lapine.Protocol;

using static System.Math;
using static System.Net.Sockets.SocketOptionLevel;
using static System.Net.Sockets.SocketOptionName;
using static Lapine.Client.ConnectionConfiguration;

abstract record ConnectionEvent;
record RemoteDisconnected(Exception Fault) : ConnectionEvent;

abstract record ConnectResult;
record Connected(IObservable<ConnectionEvent> ConnectionEvents, IObservable<RawFrame> ReceivedFrames) : ConnectResult;
record ConnectionFailed(Exception Fault) : ConnectResult;

interface ISocketAgent {
    Task<ConnectResult> ConnectAsync(IPEndPoint endpoint, CancellationToken cancellationToken = default);
    Task Tune(UInt32 maxFrameSize);
    Task EnableTcpKeepAlives(TimeSpan probeTime, TimeSpan retryInterval, Int32 retryCount);
    Task Transmit(ISerializable entity);
    Task Disconnect();
    Task StopAsync();
}

class SocketAgent : ISocketAgent {
    readonly IAgent<Protocol> _agent;

    SocketAgent(IAgent<Protocol> agent) =>
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));

    abstract record Protocol;
    record Connect(IPEndPoint Endpoint, AsyncReplyChannel ReplyChannel, CancellationToken CancellationToken = default) : Protocol;
    record Tune(UInt32 MaxFrameSize) : Protocol;
    record EnableTcpKeepAlives(TimeSpan ProbeTime, TimeSpan RetryInterval, Int32 RetryCount) : Protocol;
    record Transmit(ISerializable Entity) : Protocol;
    record Disconnect : Protocol;
    record Poll : Protocol;
    record OnAsyncResult(IAsyncResult Result) : Protocol;

    static public ISocketAgent Create() =>
        new SocketAgent(Agent<Protocol>.StartNew(Disconnected()));

    static Behaviour<Protocol> Disconnected() =>
        async context => {
            switch (context.Message) {
                case Connect(var endpoint, var replyChannel, var cancellationToken): {
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try {
                        await socket.ConnectAsync(endpoint, cancellationToken);

                        var events = new Subject<ConnectionEvent>();
                        var receivedFrames = new Subject<RawFrame>();

                        replyChannel.Reply(new Connected(events, receivedFrames));

                        // Begin polling...
                        await context.Self.PostAsync(new Poll());

                        return context with { Behaviour = ConnectedBehaviour(socket, events, receivedFrames) };
                    }
                    catch (OperationCanceledException) {
                        replyChannel.Reply(new ConnectionFailed(new TimeoutException()));
                        await context.Self.StopAsync();
                        return context;
                    }
                    catch (Exception fault) {
                        replyChannel.Reply(new ConnectionFailed(fault));
                        await context.Self.StopAsync();
                        return context;
                    }
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Disconnected)}' behaviour.");
            }
        };

    static Behaviour<Protocol> ConnectedBehaviour(Socket socket, Subject<ConnectionEvent> connectionEvents, Subject<RawFrame> receivedFrames) {
        var transmitBuffer = new MemoryBufferWriter<Byte>(4096);
        var (frameBuffer, tail) = (new Byte[DefaultMaximumFrameSize], 0);

        return context => {
            switch (context.Message) {
                case Tune(var maxFrameSize): {
                    transmitBuffer = new MemoryBufferWriter<Byte>((Int32)maxFrameSize);
                    Array.Resize(ref frameBuffer, (Int32)maxFrameSize);
                    return ValueTask.FromResult(context);
                }
                case Poll: {
                    socket.BeginReceive(
                        buffer     : frameBuffer,
                        offset     : tail,
                        size       : Min(4096, frameBuffer.Length - tail),
                        socketFlags: SocketFlags.None,
                        state      : socket,
                        callback   : asyncResult => {
                            context.Self.PostAsync(new OnAsyncResult(asyncResult));
                        }
                    );
                    return ValueTask.FromResult(context);
                }
                case OnAsyncResult(var asyncResult): {
                    try {
                        tail += socket.EndReceive(asyncResult);

                        if (tail > 0) {
                            ReadOnlySpan<Byte> buffer = frameBuffer.AsSpan(0, tail);
                            while (RawFrame.Deserialize(ref buffer, out var frame)) {
                                receivedFrames.OnNext(frame.Value);
                                buffer.CopyTo(frameBuffer); // Move any bytes that were not consumed to the front of the frame buffer...
                                tail = buffer.Length;
                                buffer = frameBuffer.AsSpan(0, tail);
                            }
                        }
                        context.Self.PostAsync(new Poll());
                        return ValueTask.FromResult(context);
                    }
                    catch (SocketException fault) when (fault.ErrorCode == 104) {
                        connectionEvents.OnNext(new RemoteDisconnected(fault));
                        socket.Disconnect(true);
                        connectionEvents.OnCompleted();
                        connectionEvents.Dispose();
                        return ValueTask.FromResult(context with { Behaviour = Disconnected() });
                    }
                }
                case Transmit(var entity): {
                    transmitBuffer.WriteSerializable(entity);
                    socket.Send(transmitBuffer.WrittenSpan);
                    transmitBuffer.Clear();
                    return ValueTask.FromResult(context);
                }
                case Disconnect: {
                    socket.Disconnect(true);
                    connectionEvents.OnCompleted();
                    connectionEvents.Dispose();
                    return ValueTask.FromResult(context with { Behaviour = Disconnected() });
                }
                case EnableTcpKeepAlives(var probeTime, var retryInterval, var retryCount): {
                    socket.SetSocketOption(SocketOptionLevel.Socket, KeepAlive, true);
                    socket.SetSocketOption(Tcp, TcpKeepAliveTime, (Int32) Round(probeTime.TotalSeconds));
                    socket.SetSocketOption(Tcp, TcpKeepAliveInterval, (Int32) Round(retryInterval.TotalSeconds));
                    socket.SetSocketOption(Tcp, TcpKeepAliveRetryCount, retryCount);
                    return ValueTask.FromResult(context);
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Connected)}' behaviour.");
            }
        };
    }

    async Task<ConnectResult> ISocketAgent.ConnectAsync(IPEndPoint endpoint, CancellationToken cancellationToken) {
        var reply = await _agent.PostAndReplyAsync(replyChannel => new Connect(endpoint, replyChannel, cancellationToken));
        return (ConnectResult) reply;
    }

    async Task ISocketAgent.Tune(UInt32 maxFrameSize) =>
        await _agent.PostAsync(new Tune(maxFrameSize));

    async Task ISocketAgent.EnableTcpKeepAlives(TimeSpan probeTime, TimeSpan retryInterval, Int32 retryCount) =>
        await _agent.PostAsync(new EnableTcpKeepAlives(probeTime, retryInterval, retryCount));

    async Task ISocketAgent.Transmit(ISerializable entity) =>
        await _agent.PostAsync(new Transmit(entity));

    async Task ISocketAgent.Disconnect() =>
        await _agent.PostAsync(new Disconnect());

    async Task ISocketAgent.StopAsync() =>
        await _agent.StopAsync();
}
