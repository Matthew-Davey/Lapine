namespace Lapine.Client;

using System.Runtime.ExceptionServices;
using Lapine.Agents;

public class AmqpClient(ConnectionConfiguration connectionConfiguration) : IAsyncDisposable {
    readonly IAmqpClientAgent _agent = AmqpClientAgent.Create();

    public async ValueTask ConnectAsync(CancellationToken cancellationToken = default) {
        switch (await _agent.EstablishConnection(connectionConfiguration, cancellationToken)) {
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
        switch (await _agent.OpenChannel(cancellationToken)) {
            case IChannelAgent channelAgent: {
                return new Channel(channelAgent, connectionConfiguration);
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
        await _agent.Disconnect();
        await _agent.Stop();

        GC.SuppressFinalize(this);
    }
}
