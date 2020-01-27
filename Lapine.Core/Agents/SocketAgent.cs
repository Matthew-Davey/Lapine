namespace Lapine.Agents {
    using System;
    using System.Buffers;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Lapine.Protocol;
    using Microsoft.Extensions.Logging;
    using Proto;

    using static Lapine.Agents.Messages;
    using static Lapine.Log;
    using static Proto.Actor;

    public class SocketAgent : IActor {
        readonly ILogger _log;
        readonly PID _listener;
        readonly Behavior _behaviour;
        readonly Socket _socket;
        readonly Memory<Byte> _frameBuffer;
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly Thread _pollThread;
        UInt16 _frameBufferSize = 0;

        public SocketAgent(PID listener) {
            _log                     = CreateLogger(GetType());
            _listener                = listener ?? throw new ArgumentNullException(nameof(listener));
            _behaviour               = new Behavior(AwaitConnect);
            _socket                  = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _frameBuffer             = new Byte[1024 * 1024 * 8];
            _cancellationTokenSource = new CancellationTokenSource();
            _pollThread              = new Thread(PollThreadMain);
        }

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task AwaitConnect(IContext context) {
            switch (context.Message) {
                case (Connect, IPEndPoint endpoint): {
                    _log.LogInformation("Attempting to connect to {endpoint}:{port}",endpoint.Address, endpoint.Port);
                    _socket.Connect(endpoint);
                    _log.LogInformation("Connection established");

                    _pollThread.Start((_cancellationTokenSource.Token, context));
                    _behaviour.Become(Connected);

                    context.Send(_listener, (SocketConnected));

                    // Transmit protocol header to start handshake process...
                    _log.LogDebug("Sending protocol header");
                    var buffer = new ArrayBufferWriter<Byte>(initialCapacity: 8);
                    ProtocolHeader.Default.Serialize(buffer);
                    _socket.Send(buffer.WrittenSpan);
                    _log.LogDebug("Transmitted {bytes} bytes", buffer.WrittenSpan.Length);
                    return Done;
                }
                default: return Done;
            }
        }

        Task Connected(IContext context) {
            switch (context.Message) {
                case (Outbound, RawFrame frame): {
                    var buffer = new ArrayBufferWriter<Byte>(initialCapacity: (Int32)frame.SerializedSize);
                    frame.Serialize(buffer);
                    _socket.Send(buffer.WrittenSpan);
                    _log.LogDebug("Transmitted {bytes} bytes", buffer.WrittenSpan.Length);
                    return Done;
                }
                case (Disconnect): {
                    context.Self.Stop();
                    return Done;
                }
                case Stopping _: {
                    if (_socket.Connected) {
                        _socket.Disconnect(false);
                        _socket.Close();
                        context.Send(_listener, (SocketDisconnected));
                    }

                    if (_pollThread.IsAlive) {
                        _cancellationTokenSource.Cancel();
                        _pollThread.Join();
                    }
                    return Done;
                }
                default: return Done;
            }
        }

        void PollThreadMain(Object state) {
            var (cancellationToken, context) = ((CancellationToken, IContext))state;

            try {
                while (cancellationToken.IsCancellationRequested == false) {
                    var bytesReceived = _socket.Receive(_frameBuffer.Slice(_frameBufferSize).Span);

                    if (bytesReceived > 0) {
                        _log.LogDebug("Received {bytes} bytes", bytesReceived);

                        _frameBufferSize += (UInt16)bytesReceived;

                        while (RawFrame.Deserialize(_frameBuffer.Slice(0, _frameBufferSize).Span, out var frame, out var surplus)) {
                            context.Send(_listener, (Inbound, frame));

                            var consumed = _frameBufferSize - surplus.Length;
                            _frameBuffer.Slice(consumed, _frameBufferSize).CopyTo(_frameBuffer);
                            _frameBufferSize -= (UInt16)consumed;
                        }
                    }
                }
            }
            catch (SocketException) {
                // TODO: Publish a disconnected event, or just let the actor die?
                // Or both?
            }
            catch (ObjectDisposedException) {
                // The socket has been closed...
            }
            catch (OperationCanceledException) {
                // Let the thread terminate...
            }
        }
    }
}
