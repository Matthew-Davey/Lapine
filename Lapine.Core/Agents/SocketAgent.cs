namespace Lapine.Agents {
    using System;
    using System.Buffers;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Lapine.Protocol;
    using Proto;

    using static Proto.Actor;

    class SocketAgent : IActor {
        readonly PID _listener;
        readonly Behavior _behaviour;
        readonly Socket _socket;
        readonly ArrayBufferWriter<Byte> _transmitBuffer;
        Task? _pollingTask;

        public SocketAgent(PID listener) {
            _listener       = listener ?? throw new ArgumentNullException(nameof(listener));
            _behaviour      = new Behavior(Unstarted);
            _socket         = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _transmitBuffer = new ArrayBufferWriter<Byte>(initialCapacity: 1024 * 1024 * 1);
        }

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Unstarted(IContext context) {
            switch (context.Message) {
                case Started _: {
                    _behaviour.Become(Disconnected);
                    break;
                }
            }
            return Done;
        }

        Task Disconnected(IContext context) {
            switch (context.Message) {
                case (":connect", IPEndPoint endpoint): {
                    _socket.Connect(endpoint);
                    _pollingTask = BeginPolling(context);
                    _behaviour.Become(Connected);

                    context.Send(_listener, (":socket-connected"));

                    // Transmit protocol header to start handshake process...
                    Transmit(ProtocolHeader.Default);
                    break;
                }
            }
            return Done;
        }

        Task Connected(IContext context) {
            switch (context.Message) {
                case (":transmit", RawFrame frame): {
                    Transmit(frame);
                    break;
                }
            }
            return Done;
        }

        void Transmit(ISerializable entity) {
            _transmitBuffer.WriteSerializable(entity);
            _socket.Send(_transmitBuffer.WrittenSpan);
            _transmitBuffer.Clear();
        }

        Task BeginPolling(IContext context) =>
            Task.Factory.StartNew(() => {
                var receiveBuffer = new Byte[1024 * 1024 * 8].AsMemory();
                var receiveBufferTail = 0;

                while (true) {
                    var bytesReceived = _socket.Receive(receiveBuffer.Slice(receiveBufferTail).Span);

                    if (bytesReceived > 0) {
                        receiveBufferTail += bytesReceived;

                        while (RawFrame.Deserialize(receiveBuffer.Slice(0, receiveBufferTail).Span, out var frame, out var surplus)) {
                            context.Send(_listener, (":receive", frame));

                            var consumed = receiveBufferTail - surplus.Length; // How many bytes of the frame buffer were consumed by deserializating this frame...
                            receiveBuffer.Slice(consumed, receiveBufferTail).CopyTo(receiveBuffer);
                            receiveBufferTail -= consumed;
                        }
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }
}
