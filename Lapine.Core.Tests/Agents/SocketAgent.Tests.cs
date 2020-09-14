namespace Lapine.Agents {
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Lapine.Protocol;
    using Bogus;
    using Proto;
    using Proto.Mailbox;
    using Xbehave;
    using Xunit;

    public class SocketAgentTests : Faker, IDisposable {
        readonly RootContext _context;
        readonly IList<Object> _sent;
        readonly PID _listener;
        readonly PID _subject;
        readonly TcpListener _tcpListener;
        readonly Int32 _port;

        public SocketAgentTests() {
            _context  = ActorSystem.Default.Root;
            _sent     = new List<Object>();
            _listener = _context.Spawn(Props.FromFunc(_ => Actor.Done));
            _subject  = _context.Spawn(
                Props.FromProducer(() => new SocketAgent(_listener))
                    .WithDispatcher(new SynchronousDispatcher())
                    .WithSenderMiddleware(next => (context, target, envelope) => {
                        _sent.Add(envelope.Message);
                        return next(context, target, envelope);
                    })
            );
            _port        = Random.Int(min: 1025, max: Int16.MaxValue);
            _tcpListener = new TcpListener(IPAddress.Loopback, _port);

            _tcpListener.Start();
        }

        [Scenario(Timeout = 1000)]
        public void EstablishesConnection(Socket socket) {
            "When the agent is instructed to connect to a remote endpoint".x(() => {
                _context.Send(_subject, (":connect", new IPEndPoint(IPAddress.Loopback, _port)));
            });
            "Then it should establish a TCP connection".x(() => {
                socket = _tcpListener.AcceptSocket();
                Assert.True(socket.Connected);
            });
            "And it should send a SocketConnected message".x(() => {
                Assert.Contains(_sent, message => message switch {
                    (":socket-connected") => true,
                    _                     => false
                });
            });
            "And it should transmit the protocol header immediately".x(() => {
                var buffer = new Byte[8];
                socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                ProtocolHeader.Deserialize(buffer, out var result, out var _);

                Assert.Equal(expected: ProtocolHeader.Default, actual: result);
            });
        }

        [Scenario]
        public void ReceivesIncomingFrames(Socket socket) {
            "Given the agent is connected to a remote endpoint".x(() => {
                _context.Send(_subject, (":connect", new IPEndPoint(IPAddress.Loopback, _port)));
                socket = _tcpListener.AcceptSocket();
            });
            "When the remote endpoint sends a frame".x(() => {
                var frame = new RawFrame(FrameType.Heartbeat, 0, new Byte[0]);
                var writer = new ArrayBufferWriter<Byte>();
                frame.Serialize(writer);

                socket.Send(writer.WrittenSpan);
            });
            "Then it should send an Inbound Frame message".x(async () => {
                await Task.Delay(10);
                Assert.Contains(_sent, message => message switch {
                    (":receive", RawFrame _) => true,
                    _                        => false
                });
            });
        }

        public void Dispose() {
            _tcpListener.Stop();
            _context.Stop(_subject);
        }
    }
}
