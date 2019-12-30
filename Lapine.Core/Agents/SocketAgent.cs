namespace Lapine.Agents {
    using System;
    using System.Buffers;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Lapine.Agents.Events;
    using Lapine.Protocol;
    using Microsoft.Extensions.Logging;
    using Proto;

    using static Lapine.Agents.Commands;
    using static Lapine.Direction;
    using static Lapine.Log;
    using static Proto.Actor;

    public class SocketAgent : IActor {
        readonly ILogger _log;
        readonly Behavior _behaviour;
        readonly Socket _socket;
        readonly Memory<Byte> _frameBuffer;
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly Thread _pollThread;
        UInt16 _frameBufferSize = 0;

        public SocketAgent() {
            _log                     = CreateLogger(GetType());
            _behaviour               = new Behavior(Disconnected);
            _socket                  = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _frameBuffer             = new Byte[1024 * 1024 * 8];
            _cancellationTokenSource = new CancellationTokenSource();
            _pollThread              = new Thread(PollThreadMain);
        }

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Disconnected(IContext context) {
            switch (context.Message) {
                case (Connect, IPEndPoint endpoint): {
                    _log.LogInformation("Attempting to connect to {endpoint}:{port}",endpoint.Address, endpoint.Port);
                    _socket.Connect(endpoint);
                    _log.LogInformation("Connection estabished");

                    _pollThread.Start((_cancellationTokenSource.Token, context));
                    _behaviour.Become(Connected);

                    context.Send(context.Parent, new SocketConnected());

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
                case Stopping _: {
                    _cancellationTokenSource.Cancel();
                    _pollThread.Join();
                    _socket.Close();
                    _behaviour.Become(Stopped);
                    return Done;
                }
                default: return Done;
            }
        }

        Task Stopped(IContext _) => Done;

        void PollThreadMain(Object state) {
            var (cancellationToken, context) = ((CancellationToken, IContext))state;

            try {
                while (cancellationToken.IsCancellationRequested == false) {
                    var bytesReceived = _socket.Receive(_frameBuffer.Slice(_frameBufferSize).Span);

                    if (bytesReceived > 0) {
                        _log.LogDebug("Received {bytes} bytes", bytesReceived);

                        _frameBufferSize += (UInt16)bytesReceived;

                        while (RawFrame.Deserialize(_frameBuffer.Slice(0, _frameBufferSize).Span, out var frame, out var surplus)) {
                            context.Send(context.Parent, (Inbound, frame));

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
            catch (OperationCanceledException) {
                // Let the thread terminate...
            }
        }
    }
}
