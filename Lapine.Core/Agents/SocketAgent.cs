namespace Lapine.Agents {
    using System;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Lapine.Agents.Commands;
    using Lapine.Agents.Events;
    using Proto;
    using Proto.Schedulers.SimpleScheduler;

    using static Proto.Actor;

    public class SocketAgent : IActor {
        readonly Behavior _behaviour;
        readonly TcpClient _socket;
        ISimpleScheduler _pollScheduler;

        public SocketAgent() {
            _behaviour     = new Behavior(Disconnected);
            _socket        = new TcpClient();
            _pollScheduler = new SimpleScheduler();
        }

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Disconnected(IContext context) {
            switch (context.Message) {
                case SocketConnect message: {
                    _socket.Connect(message.IpAddress, message.Port);

                    _pollScheduler = new SimpleScheduler(context);
                    _pollScheduler.ScheduleTellOnce(delay: TimeSpan.Zero, target: context.Self, message: new SocketPoll());

                    _behaviour.Become(Connected);
                    return Done;
                }
                default: return Done;
            }
        }

        Task Connected(IContext context) {
            switch (context.Message) {
                case SocketPoll message: {
                    switch (_socket.Available) {
                        case 0: {
                            // There was no data available - back off polling for 50 millis...
                            _pollScheduler.ScheduleTellOnce(delay: TimeSpan.FromMilliseconds(50), target: context.Self, message);
                            break;
                        }
                        case var available: {
                            var buffer = new Byte[available].AsSpan();
                            var bytesReceived = _socket.GetStream().Read(buffer);

                            Actor.EventStream.Publish(new SocketDataReceived(buffer.Slice(0, bytesReceived).ToArray()));

                            // Data was received - poll again immediately...
                            _pollScheduler.ScheduleTellOnce(delay: TimeSpan.Zero, target: context.Self, message);
                            break;
                        }
                    }
                    return Done;
                }
                case SocketTransmit message: {
                    _socket.GetStream().Write(message.Buffer, 0, message.Buffer.Length);
                    return Done;
                }
                case Proto.Stopping _: {
                    _socket.Close();
                    _behaviour.Become(Stopped);
                    return Done;
                }
                default: return Done;
            }
        }

        Task Stopped(IContext _) => Done;
    }
}
