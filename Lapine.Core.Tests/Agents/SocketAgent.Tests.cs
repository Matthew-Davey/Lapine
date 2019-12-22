namespace Lapine.Agents {
    using System;
    using System.Buffers;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Lapine.Agents.Commands;
    using Lapine.Agents.Events;
    using Lapine.Protocol;
    using Bogus;
    using Proto;
    using Xunit;

    public class SocketAgentTests : Faker, IDisposable {
        readonly RootContext _context;
        readonly PID _subject;
        readonly TcpListener _listener;

        public SocketAgentTests() {
            _context  = new RootContext();
            _subject  = _context.Spawn(Props.FromProducer(() => new SocketAgent()));
            _listener = new TcpListener(IPAddress.Loopback, 5678);

            _listener.Start();
        }

        [Fact]
        public void EstablishesConnection() {
            _context.Send(_subject, new SocketConnect(IPAddress.Loopback, 5678));

            var socket = _listener.AcceptSocket();
            Assert.True(socket.Connected);
        }

        [Fact]
        public void TransmitsData() {
            var message = ProtocolHeader.Default;

            _context.Send(_subject, new SocketConnect(IPAddress.Loopback, 5678));
            _context.Send(_subject, new SocketTransmit(message));

            var socket = _listener.AcceptSocket();
            var buffer = new Byte[8];
            socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
            ProtocolHeader.Deserialize(buffer, out var result, out var _);

            Assert.Equal(expected: message, actual: result);
        }

        [Fact]
        public void ReceivesSingleFrame() {
            var receivedFrame = default(RawFrame);
            var receivedEvent = new ManualResetEventSlim(initialState: false);

            var subscription = Actor.EventStream.Subscribe<FrameReceived>(message => {
                receivedFrame = message.Frame;
                receivedEvent.Set();
            });

            _context.Send(_subject, new SocketConnect(IPAddress.Loopback, 5678));

            var buffer = new ArrayBufferWriter<Byte>();
            var sentFrame  = new RawFrame(Random.Enum<FrameType>(), Random.UShort(), Random.Bytes(Random.UShort()));
            sentFrame.Serialize(buffer);
            var client = _listener.AcceptTcpClient();
            client.GetStream().Write(buffer.WrittenMemory.Span);

            // Wait for the agent to receive the frame, or time out...
            if (receivedEvent.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.Equal(expected: sentFrame.Channel, actual: receivedFrame.Channel);
                Assert.Equal(expected: sentFrame.Payload.ToArray(), actual: receivedFrame.Payload.ToArray());
                Assert.Equal(expected: sentFrame.Size, actual: receivedFrame.Size);
                Assert.Equal(expected: sentFrame.Type, actual: receivedFrame.Type);
            }
            else {
                // No `FrameReceived` event was published within 100 millis...
                throw new TimeoutException("Timeout occurred before trasmitted frame was received");
            }
        }

        public void Dispose() {
            _listener.Stop();
            _subject.Stop();
        }
    }
}
