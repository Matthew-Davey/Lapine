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
        readonly Int32 _port;

        public SocketAgentTests() {
            _context  = new RootContext();
            _subject  = _context.Spawn(Props.FromProducer(() => new SocketAgent()));
            _port     = Random.Int(min: 1025, max: Int16.MaxValue);
            _listener = new TcpListener(IPAddress.Loopback, _port);

            _listener.Start();
        }

        [Fact]
        public void EstablishesConnection() {
            _context.Send(_subject, (Connect, new IPEndPoint(IPAddress.Loopback, _port)));

            var socket = _listener.AcceptSocket();
            Assert.True(socket.Connected);
        }

        [Fact(Timeout = 1000)]
        public void TransmitsProtocolHeader() {
            _context.Send(_subject, (Connect, new IPEndPoint(IPAddress.Loopback, _port)));

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
