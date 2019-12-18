namespace Lapine.Agents {
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Lapine.Agents.Commands;
    using Proto;

    using static Proto.Actor;

    public class SocketAgent : IActor {
        readonly Behavior _behaviour;
        readonly TcpClient _socket;

        public SocketAgent() {
            _behaviour = new Behavior(Disconnected);
            _socket    = new TcpClient();
        }

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Disconnected(IContext context) {
            switch (context.Message) {
                case SocketConnect message: {
                    _socket.Connect(message.IpAddress, message.Port);
                    _behaviour.Become(Connected);
                    return Done;
                }
                default: return Done;
            }
        }

        Task Connected(IContext context) {
            switch (context.Message) {
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
