namespace Lapine.Client {
    using System;
    using System.Threading.Tasks;
    using Lapine.Agents;
    using Proto;

    using static Lapine.Agents.RabbitClientAgent.Protocol;

    public class AmqpClient : IAsyncDisposable {
        readonly ActorSystem _system;
        readonly ConnectionConfiguration _connectionConfiguration;
        readonly PID _agent;

        public AmqpClient(ConnectionConfiguration connectionConfiguration) {
            _system = new ActorSystem();
            _connectionConfiguration = connectionConfiguration;
            _agent = _system.Root.SpawnNamed(
                name: "amqp-client",
                props: RabbitClientAgent.Create()
            );
        }

        public async ValueTask ConnectAsync(TimeSpan? timeout = default) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new EstablishConnection(
                Configuration: _connectionConfiguration with {
                    ConnectionTimeout = timeout ?? _connectionConfiguration.ConnectionTimeout
                }, promise
            ));
            await promise.Task;
        }

        public async ValueTask<Channel> OpenChannelAsync() {
            var promise = new TaskCompletionSource<PID>();
            _system.Root.Send(_agent, new OpenChannel(promise));

            return new Channel(_system, await promise.Task);
        }

        public async ValueTask DisposeAsync() =>
            await _system.Root.StopAsync(_agent);
    }
}
