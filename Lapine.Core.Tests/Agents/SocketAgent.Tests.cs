namespace Lapine.Agents {
    using System;
    using System.Net;
    using System.Net.Sockets;
    using Lapine.Agents.Commands;
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
            var message = UTF8.GetBytes("test message");

            _context.Send(_subject, new SocketConnect(IPAddress.Loopback, 5678));
            _context.Send(_subject, new SocketTransmit(message));

            var socket = _listener.AcceptSocket();
            var result = new Byte[message.Length];
            socket.Receive(result, 0, result.Length, SocketFlags.None);

            Assert.Equal(expected: message, actual: result);
        }

        public void Dispose() =>
            _listener.Stop();
    }
}
