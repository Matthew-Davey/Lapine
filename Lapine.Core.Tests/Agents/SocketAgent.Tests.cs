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

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.SocketAgent.Protocol;

    public class SocketAgentTests : Faker, IDisposable {
        readonly ActorSystem _system;
        readonly RootContext _context;
        readonly IList<Object> _sent;
        readonly PID _listener;
        readonly PID _subject;
        readonly TcpListener _tcpListener;
        readonly Int32 _port;

        public SocketAgentTests() {
            _system   = new ActorSystem();
            _context  = _system.Root;
            _sent     = new List<Object>();
            _listener = _context.Spawn(Props.FromFunc(_ => CompletedTask));
            _subject  = _context.Spawn(
                SocketAgent.Create()
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
                _context.Send(_subject, new Connect(new IPEndPoint(IPAddress.Loopback, _port), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(20), _listener));
            });
            "Then is should publish a Connecting event".x(() => {
                Assert.Contains(new Connecting(), _sent);
            });
            "Then it should establish a TCP connection".x(() => {
                socket = _tcpListener.AcceptSocket();
                Assert.True(socket.Connected);
            });
            "Then it should publish a Connected message".x(() => {
                Assert.Contains(new Connected(), _sent);
            });
        }

        [Scenario]
        public void ReceivesIncomingFrames(Socket socket) {
            "Given the agent is connected to a remote endpoint".x(() => {
                _context.Send(_subject, new Connect(new IPEndPoint(IPAddress.Loopback, _port), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(20), _listener));
                socket = _tcpListener.AcceptSocket();
            });
            "When the remote endpoint sends a frame".x(() => {
                var writer = new ArrayBufferWriter<Byte>();
                RawFrame.Heartbeat.Serialize(writer);

                socket.Send(writer.WrittenSpan);
            });
            "Then it should publish a FrameReceived event".x(async () => {
                await Task.Delay(100);
                Assert.Contains(new FrameReceived(RawFrame.Heartbeat), _sent);
            });
        }

        public void Dispose() {
            _tcpListener.Stop();
            _context.Stop(_subject);
            GC.SuppressFinalize(this);
        }
    }
}
