namespace Lapine.Client;

using System.Runtime.ExceptionServices;
using Lapine.Agents;

using static Lapine.Agents.AmqpClientAgent.Protocol;

public class AmqpClient : IAsyncDisposable {
    readonly ConnectionConfiguration _connectionConfiguration;
    readonly IAgent _agent;

    public AmqpClient(ConnectionConfiguration connectionConfiguration) {
        _connectionConfiguration = connectionConfiguration;
        _agent = AmqpClientAgent.Create();
    }

    public async ValueTask ConnectAsync(CancellationToken cancellationToken = default) {
        var command = new EstablishConnection(_connectionConfiguration, cancellationToken);

        switch (await _agent.PostAndReplyAsync(command)) {
            case true: {
                return;
            }
            case Exception fault: {
                ExceptionDispatchInfo
                    .Capture(fault)
                    .Throw();
                return;
            }
        }
    }

    public async ValueTask<Channel> OpenChannelAsync(CancellationToken cancellationToken = default) {
        var command = new OpenChannel(cancellationToken);

        switch (await _agent.PostAndReplyAsync(command)) {
            case IAgent channelAgent: {
                return new Channel(channelAgent, _connectionConfiguration);
            }
            case Exception fault: {
                ExceptionDispatchInfo
                    .Capture(fault)
                    .Throw();
                return null;
            }
            case var message: throw new Exception($"Unexpected message '{message.GetType().FullName}' in '{nameof(OpenChannelAsync)}' method.");
        }
    }

    public async ValueTask DisposeAsync() {
        await _agent.PostAndReplyAsync(new Disconnect());
        await _agent.StopAsync();

        GC.SuppressFinalize(this);
    }
}
