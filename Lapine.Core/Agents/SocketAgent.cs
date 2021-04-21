namespace Lapine.Agents {
    using System;
    using System.Buffers;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Lapine.Protocol;
    using Proto;
    using Proto.Timers;

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.SocketAgent.Protocol;
    using static Lapine.Client.ConnectionConfiguration;

    static class SocketAgent {
        static public class Protocol {
            public record Connect(IPEndPoint Endpoint, TimeSpan ConnectTimeout, PID Listener);
            public record Connecting();
            public record Connected(PID TxD, PID RxD);
            public record ConnectionFailed(Exception Reason);
            public record Tune(UInt32 MaxFrameSize);
            public record Transmit(ISerializable Entity);
            public record BeginPolling(PID FrameListener);
            public record FrameReceived(RawFrame Frame);

            internal record TimeoutExpired();
            internal record Bind(Socket Socket);
            internal record Poll();
        }

        static public Props Create() =>
            Props.FromProducer(() => new Actor());

        class Actor : IActor {
            readonly Behavior _behaviour;

            public Actor() {
                _behaviour = new Behavior(Disconnected);
            }

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
                        var cancelTimeout = context.Scheduler().SendOnce(connect.ConnectTimeout, context.Self!, new TimeoutExpired());
                        _behaviour.Become(Connecting(connect.Listener, socket, cancelTimeout));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive Connecting(PID listener, Socket socket, CancellationTokenSource cancelTimeout) =>
                (IContext context) => {
                    switch (context.Message) {
                        case TimeoutExpired timeout: {
                            socket.Close();
                            context.Send(listener, new ConnectionFailed(new TimeoutException()));
                            context.Stop(context.Self!);
                            break;
                        }
                        case IAsyncResult asyncResult: {
                            cancelTimeout.Cancel();
                            try {
                                socket.EndConnect(asyncResult);
                                var txd = context.SpawnNamed(
                                    name: "txd",
                                    props: Props.FromProducer(() => new TxD())
                                );
                                var rxd = context.SpawnNamed(
                                    name: "rxd",
                                    props: Props.FromProducer(() => new RxD())
                                );
                                context.Send(txd, new Bind(socket));
                                context.Send(rxd, new Bind(socket));
                                context.Send(listener, new Connected(txd, rxd));

                                _behaviour.Become(Connected(socket));
                            }
                            catch (SocketException error) {
                                socket.Close();
                                context.Send(listener, new ConnectionFailed(error));
                                context.Stop(context.Self!);
                            }
                            break;
                        }
                    };
                    return CompletedTask;
                };

            static Receive Connected(Socket socket) =>
                (IContext context) => {
                    switch (context.Message) {
                        case Tune x: {
                            foreach (var child in context.Children) {
                                context.Forward(child);
                            }
                            break;
                        }
                        case Restarting:
                        case Stopping _: {
                            socket.Close();
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }

        class TxD : IActor {
            readonly Behavior _behaviour;

            public TxD() =>
                _behaviour = new Behavior(Unbound);

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Unbound(IContext context) {
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
            static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(10);

            readonly Behavior _behaviour;

            public RxD() =>
                _behaviour = new Behavior(Unbound);

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Unbound(IContext context) {
                switch (context.Message) {
                    case Bind bind: {
                        _behaviour.Become(Ready(bind.Socket));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive Ready(Socket socket) =>
                (IContext context) => {
                    switch (context.Message) {
                        case BeginPolling poll: {
                            context.Scheduler().SendOnce(PollingInterval, context.Self!, new Poll());
                            _behaviour.Become(Polling(socket, poll.FrameListener));
                            break;
                        }
                    }
                    return CompletedTask;
                };

            static Receive Polling(Socket socket, PID frameListener) {
                // The socket receive buffer is considerably smaller than the max frame size, so we accumulate
                // received bytes in the frame buffer until we can deserialize one or more AMQP frames...
                var (frameBuffer, tail) = (new Byte[DefaultMaximumFrameSize], 0);

                return (IContext context) => {
                    switch (context.Message) {
                        case Poll poll: {
                            socket.BeginReceive(frameBuffer, tail, Math.Min(128, frameBuffer.Length - tail), SocketFlags.None, asyncResult => {
                                context.Send(context.Self!, asyncResult);
                            }, null);
                            break;
                        }
                        case IAsyncResult asyncResult: {
                            tail += socket.EndReceive(asyncResult);

                            if (tail > 0) {
                                while (RawFrame.Deserialize(frameBuffer.AsSpan(0, tail), out var frame, out var remaining)) {
                                    context.Send(frameListener, new FrameReceived(frame));
                                    remaining.CopyTo(frameBuffer); // Move any bytes that were not consumed to the front of the frame buffer...
                                    tail = remaining.Length;
                                }

                                context.Send(context.Self!, new Poll());
                            }
                            else {
                                context.Scheduler().SendOnce(PollingInterval, context.Self!, new Poll());
                            }
                            break;
                        }
                        case Tune tune: {
                            Array.Resize(ref frameBuffer, (Int32)tune.MaxFrameSize);
                            break;
                        }
                    }
                    return CompletedTask;
                };
            }
        }
    }
}
