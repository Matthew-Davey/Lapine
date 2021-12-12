namespace Lapine.Client;

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
        var command = new EstablishConnection(
            Configuration: _connectionConfiguration with {
                ConnectionTimeout = timeout ?? _connectionConfiguration.ConnectionTimeout
            }
        );
        _system.Root.Send(_agent, command);
        await command;
    }

    public async ValueTask<Channel> OpenChannelAsync(TimeSpan? timeout = null) {
        var command = new OpenChannel(timeout ?? _connectionConfiguration.CommandTimeout);
        _system.Root.Send(_agent, command);

        return new Channel(_system, await command, _connectionConfiguration);
    }

    public async ValueTask DisposeAsync() =>
        await _system.Root.StopAsync(_agent);
}
