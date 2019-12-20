namespace Lapine.Agents {
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Lapine.Agents.Commands;
    using Lapine.Agents.Events;
    using Proto;
    using Xunit;

    using static System.Text.Encoding;

    public class SocketAgentTests : IDisposable {
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

            socket.Dispose();
        }

        [Fact]
        public void TransmitsData() {
            var message = UTF8.GetBytes("tx");

            _context.Send(_subject, new SocketConnect(IPAddress.Loopback, 5678));
            _context.Send(_subject, new SocketTransmit(message));

            var socket = _listener.AcceptSocket();
            var result = new Byte[message.Length];
            socket.Receive(result, 0, result.Length, SocketFlags.None);

            Assert.Equal(expected: message, actual: result);
        }

        [Fact]
        public void ReceivesData() {
            var receivedBytes = default(Byte[]);
            var receivedEvent = new ManualResetEventSlim(initialState: false);

            var subscription = Actor.EventStream.Subscribe<SocketDataReceived>(message => {
                receivedBytes = message.Buffer;
                receivedEvent.Set();
            });

            _context.Send(_subject, new SocketConnect(IPAddress.Loopback, 5678));

            var message = UTF8.GetBytes("rx");
            var client = _listener.AcceptTcpClient();
            client.GetStream().Write(message);

            // Wait for the agent to receive the data, or time out...
            if (receivedEvent.Wait(timeout: TimeSpan.FromMilliseconds(100))) {
                Assert.Equal(expected: message, actual: receivedBytes);
            }
            else {
                // No `SocketDataReceived` event was published within 100 millis...
                throw new TimeoutException("Timeout occurred before trasmitted message was received");
            }
        }

        public void Dispose() =>
            _listener.Stop();
    }
}
