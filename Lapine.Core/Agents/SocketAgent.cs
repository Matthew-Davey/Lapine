namespace Lapine.Agents {
    using System;
    using System.Buffers;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Lapine.Agents.Middleware;
    using Lapine.Protocol;
    using Proto;
    using Proto.Timers;

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.SocketAgent.Protocol;
    using static Lapine.ConnectionConfiguration;

    static class SocketAgent {
        static public class Protocol {
            public record Connect(IPEndPoint Endpoint, TimeSpan ConnectTimeout, TimeSpan PollInterval, PID Listener);
            public record Connecting();
            public record Connected();
            public record ConnectionFailed(Exception Reason);
            public record Disconnect();
            public record Disconnected();
            public record Tune(UInt32 MaxFrameSize);
            public record Transmit(ISerializable Entity);
            public record FrameReceived(RawFrame Frame);

            internal record TimeoutExpired(Socket Socket);
            internal record Bind(Socket Socket);
            internal record BeginPolling(Socket Socket, TimeSpan Interval, PID FrameListener);
            internal record Poll();
        }

        static public Props Create() =>
            Props.FromProducer(() => new Actor());

        class Actor : IActor {
            readonly Behavior _behaviour;

            public Actor() =>
                _behaviour = new Behavior(Disconnected);

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Disconnected(IContext context) {
                switch (context.Message) {
                    case Connect connect: {
                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        context.Send(connect.Listener, new Connecting());
                        socket.BeginConnect(connect.Endpoint, (asyncResult) => {
                            context.Send(context.Self!, asyncResult);
                        }, socket);
                        var cancelTimeout = context.Scheduler().SendOnce(connect.ConnectTimeout, context.Self!, new TimeoutExpired(socket));
                        _behaviour.Become(Connecting(connect.PollInterval, connect.Listener, cancelTimeout));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive Connecting(TimeSpan pollInterval, PID listener, CancellationTokenSource cancelTimeout) =>
                (IContext context) => {
                    switch (context.Message) {
                        case TimeoutExpired timeout: {
                            timeout.Socket.Close();
                            context.Send(listener, new ConnectionFailed(new TimeoutException()));
                            _behaviour.Become(Disconnected);
                            break;
                        }
                        case IAsyncResult asyncResult: {
                            cancelTimeout.Cancel();
                            var socket = (Socket)asyncResult.AsyncState!;
                            try {
                                socket.EndConnect(asyncResult);
                                var txd = context.SpawnNamed(
                                    name: "txd",
                                    props: Props.FromProducer(() => new TxD())
                                        .WithContextDecorator(LoggingContextDecorator.Create)
                                );
                                var rxd = context.SpawnNamed(
                                    name: "rxd",
                                    props: Props.FromProducer(() => new RxD())
                                        //.WithContextDecorator(LoggingContextDecorator.Create)
                                );
                                // The order here is quite important. The txd agent needs to be ready to transmit in
                                // case the listener decides to transmit a frame in response to the Connected event.
                                // But the rxd agent should not begin polling until the listener has been notified
                                // of the Connected event to ensure that it is ready to receive inbound frames...
                                context.Send(txd, new Bind(socket));
                                context.Send(listener, new Connected());
                                context.Send(rxd, new BeginPolling(socket, pollInterval, context.Self!));

                                _behaviour.Become(Connected(socket, listener, txd, rxd));
                            }
                            catch (SocketException error) {
                                socket.Close();
                                context.Send(listener, new ConnectionFailed(error));
                                _behaviour.Become(Disconnected);
                            }
                            break;
                        }
                    };
                    return CompletedTask;
                };

            Receive Connected(Socket socket, PID listener, PID txd, PID rxd) =>
                (IContext context) => {
                    switch (context.Message) {
                        case Transmit _: {
                            context.Forward(txd);
                            break;
                        }
                        case FrameReceived received: {
                            context.Forward(listener);
                            break;
                        }
                        case Tune x: {
                            context.Forward(txd);
                            context.Forward(rxd);
                            break;
                        }
                        case Terminated _:
                        case Disconnect disconnect: {
                            socket.Close();
                            context.Send(listener, new Disconnected());
                            context.Stop(txd);
                            context.Stop(rxd);
                            _behaviour.Become(Disconnected);
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }

        class TxD : IActor {
            readonly Behavior _behaviour;

            public TxD() =>
                _behaviour = new Behavior(Waiting);

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Waiting(IContext context) {
                switch (context.Message) {
                    case Bind bind: {
                        _behaviour.Become(Ready(bind.Socket));
                        break;
                    }
                }
                return CompletedTask;
            }

            static Receive Ready(Socket socket) {
                var buffer = new ArrayBufferWriter<Byte>((Int32)DefaultMaximumFrameSize);

                return (IContext context) => {
                    switch (context.Message) {
                        case Tune tune: {
                            buffer = new ArrayBufferWriter<Byte>((Int32)tune.MaxFrameSize);
                            break;
                        }
                        case Transmit transmit: {
                            buffer.WriteSerializable(transmit.Entity);
                            socket.Send(buffer.WrittenSpan);
                            buffer.Clear();
                            break;
                        }
                    }
                    return CompletedTask;
                };
            }
        }

        class RxD : IActor {
            readonly Behavior _behaviour;

            public RxD() =>
                _behaviour = new Behavior(Waiting);

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Waiting(IContext context) {
                switch (context.Message) {
                    case BeginPolling poll: {
                        context.Scheduler().SendOnce(poll.Interval, context.Self!, new Poll());
                        _behaviour.Become(Polling(poll.Socket, poll.Interval, poll.FrameListener));
                        break;
                    }
                }
                return CompletedTask;
            }

            static Receive Polling(Socket socket, TimeSpan interval, PID frameListener) {
                // The socket receive buffer is considerably smaller than the max frame size, so we accumulate
                // received bytes in the frame buffer until we can deserialize one or more AMQP frames...
                var (frameBuffer, tail) = (new Byte[DefaultMaximumFrameSize].AsMemory(), 0);

                return (IContext context) => {
                    switch (context.Message) {
                        case Poll poll: {
                                if (socket.Available == 0) {
                                    context.Scheduler().SendOnce(interval, context.Self!, poll);
                                    break;
                                }

                                tail += socket.Receive(frameBuffer[tail..].Span);

                                var remaining = (ReadOnlySpan<Byte>)frameBuffer[..tail].Span;
                                while (RawFrame.Deserialize(remaining, out var frame, out remaining)) {
                                    context.Send(frameListener, new FrameReceived(frame));
                                }
                                remaining.CopyTo(frameBuffer.Span); // Move any bytes that were not consumed to the front of the frame buffer...
                                tail = remaining.Length;

                                context.Send(context.Self!, poll);
                                break;
                        }
                        case Tune tune: {
                            var newFrameBuffer = new Byte[tune.MaxFrameSize];
                            frameBuffer[..tail].CopyTo(newFrameBuffer);
                            frameBuffer = newFrameBuffer;
                            break;
                        }
                    }
                    return CompletedTask;
                };
            }
        }
    }
}
