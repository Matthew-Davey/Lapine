namespace Lapine.Agents {
    using System;
    using System.Net;
    using System.Net.Sockets;
    using Lapine.Protocol;
    using Bogus;
    using Proto;
    using Xunit;

    using static Lapine.Agents.Commands;

    [Collection("Agents")]
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
            _context.Send(_subject, (Connect, new IPEndPoint(IPAddress.Loopback, 5678)));

            var socket = _listener.AcceptSocket();
            Assert.True(socket.Connected);
        }

        [Fact]
        public void TransmitsData() {
            _context.Send(_subject, (Connect, new IPEndPoint(IPAddress.Loopback, 5678)));
            _context.Send(_subject, ProtocolHeader.Default);

            var socket = _listener.AcceptSocket();
            var buffer = new Byte[8];
            socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
            ProtocolHeader.Deserialize(buffer, out var result, out var _);

            Assert.Equal(expected: ProtocolHeader.Default, actual: result);
        }

        public void Dispose() {
            _listener.Stop();
            _subject.Stop();
        }
    }
}
