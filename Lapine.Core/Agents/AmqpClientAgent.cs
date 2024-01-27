namespace Lapine.Agents;

using System.Collections.Immutable;
using System.Net;
using System.Reactive.Linq;
using Lapine.Client;
using Lapine.Protocol;

interface IAmqpClientAgent {
    Task<Object> EstablishConnection(ConnectionConfiguration configuration, CancellationToken cancellationToken = default);
    Task<Object> OpenChannel(CancellationToken cancellationToken = default);
    Task<Object> Disconnect();
    Task Stop();
}

class AmqpClientAgent : IAmqpClientAgent {
    readonly IAgent<Protocol> _agent;

    AmqpClientAgent(IAgent<Protocol> agent) =>
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));

    abstract record Protocol;
    record EstablishConnection(ConnectionConfiguration Configuration, AsyncReplyChannel ReplyChannel, CancellationToken CancellationToken = default) : Protocol;
    record OpenChannel(AsyncReplyChannel ReplyChannel, CancellationToken CancellationToken = default) : Protocol;
    record Disconnect(AsyncReplyChannel ReplyChannel) : Protocol;
    record HeartbeatEventEventReceived(Object Message) : Protocol;

    static public IAmqpClientAgent Create() =>
        new AmqpClientAgent(Agent<Protocol>.StartNew(Disconnected()));

    static Behaviour<Protocol> Disconnected() =>
        async context => {
            switch (context.Message) {
                case EstablishConnection(var connectionConfiguration, var replyChannel, var cancellationToken): {
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(connectionConfiguration.ConnectionTimeout);

                    var remainingEndpoints = new Queue<IPEndPoint>(connectionConfiguration.GetConnectionSequence());

                    if (remainingEndpoints.Count == 0) {
                        replyChannel.Reply(new Exception("No endpoints specified in connection configuration"));
                        return context;
                    }

                    var accumulatedFailures = new List<Exception>();

                    while (remainingEndpoints.Any()) {
                        var endpoint = remainingEndpoints.Dequeue();
                        var socketAgent = SocketAgent.Create();

                        switch (await socketAgent.ConnectAsync(endpoint, cts.Token)) {
                            case ConnectionFailed(var fault) when remainingEndpoints.Any(): {
                                accumulatedFailures.Add(fault);
                                continue;
                            }
                            case ConnectionFailed(var fault): {
                                accumulatedFailures.Add(fault);
                                replyChannel.Reply(new AggregateException("Could not connect to any of the configured endpoints", accumulatedFailures));
                                return context;
                            }
                            case Connected(var connectionEvents, var receivedFrames): {
                                var dispatcher = DispatcherAgent.Create();
                                await dispatcher.DispatchTo(socketAgent, 0);

                                var handshakeAgent = HandshakeAgent.Create(
                                    receivedFrames   : receivedFrames,
                                    connectionEvents : connectionEvents,
                                    dispatcher       : dispatcher,
                                    cancellationToken: cts.Token
                                );

                                switch (await handshakeAgent.StartHandshake(connectionConfiguration)) {
                                    case HandshakeAgent.ConnectionAgreed(var connectionAgreement): {
                                        await socketAgent.Tune(connectionAgreement.MaxFrameSize);

                                        var heartbeatAgent = HeartbeatAgent.Create();

                                        if (connectionConfiguration.ConnectionIntegrityStrategy.HeartbeatFrequency.HasValue) {
                                            var heartbeatEvents = await heartbeatAgent.Start(receivedFrames, dispatcher, connectionConfiguration.ConnectionIntegrityStrategy.HeartbeatFrequency.Value);
                                            heartbeatEvents.Subscribe(onNext: message => context.Self.PostAsync(new HeartbeatEventEventReceived(message)));
                                        }

                                        // If tcp keepalives are enabled, configure the socket...
                                        if (connectionConfiguration.ConnectionIntegrityStrategy.KeepAliveSettings.HasValue) {
                                            var (probeTime, retryInterval, retryCount) = connectionConfiguration.ConnectionIntegrityStrategy.KeepAliveSettings.Value;

                                            await socketAgent.EnableTcpKeepAlives(probeTime, retryInterval, retryCount);
                                        }

                                        replyChannel.Reply(true);

                                        return context with {
                                            Behaviour = Connected(
                                                connectionConfiguration: connectionConfiguration,
                                                socketAgent            : socketAgent,
                                                heartbeatAgent         : heartbeatAgent,
                                                receivedFrames         : receivedFrames,
                                                connectionEvents       : connectionEvents,
                                                dispatcher             : dispatcher,
                                                availableChannelIds    : Enumerable.Range(1, connectionAgreement.MaxChannelCount)
                                                    .Select(channelId => (UInt16) channelId)
                                                    .ToImmutableList()
                                            )
                                        };
                                    }
                                    case HandshakeAgent.HandshakeFailed(var fault): {
                                        replyChannel.Reply(fault);
                                        return context;
                                    }
                                }

                                break;
                            }
                        }
                    }
                    return context;
                }
                case Disconnect(var replyChannel): {
                    replyChannel.Reply(true);
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Disconnected)}' behaviour.");
            }
        };

    static Behaviour<Protocol> Connected(ConnectionConfiguration connectionConfiguration, ISocketAgent socketAgent, IHeartbeatAgent heartbeatAgent, IObservable<RawFrame> receivedFrames, IObservable<Object> connectionEvents, IDispatcherAgent dispatcher, IImmutableList<UInt16> availableChannelIds) =>
        async context => {
            switch (context.Message) {
                case HeartbeatEventEventReceived(RemoteFlatline): {
                    await heartbeatAgent.Stop();
                    await dispatcher.Stop();
                    await socketAgent.Disconnect();
                    await socketAgent.StopAsync();

                    return context with { Behaviour = Disconnected() };
                }
                case Disconnect(var replyChannel): {
                    await heartbeatAgent.Stop();
                    await dispatcher.Stop();
                    await socketAgent.Disconnect();
                    await socketAgent.StopAsync();

                    replyChannel.Reply(true);

                    return context;
                }
                case OpenChannel(var replyChannel, var cancellationToken): {
                    var channelId = availableChannelIds[0];
                    var channelAgent = ChannelAgent.Create(connectionConfiguration.MaximumFrameSize);

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(connectionConfiguration.CommandTimeout);

                    switch (await channelAgent.Open(channelId,receivedFrames.Where(frame => frame.Channel == channelId), connectionEvents, socketAgent, cts.Token)) {
                        case true: {
                            replyChannel.Reply(channelAgent);
                            return context with {
                                Behaviour = Connected(connectionConfiguration, socketAgent, heartbeatAgent, receivedFrames, connectionEvents, dispatcher, availableChannelIds.Remove(channelId))
                            };
                        }
                        case Exception fault: {
                            replyChannel.Reply(fault);
                            break;
                        }
                    }

                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Connected)}' behaviour.");
            }
        };

    async Task<Object> IAmqpClientAgent.EstablishConnection(ConnectionConfiguration configuration, CancellationToken cancellationToken) =>
        await _agent.PostAndReplyAsync(replyChannel => new EstablishConnection(configuration, replyChannel, cancellationToken));

    async Task<Object> IAmqpClientAgent.OpenChannel(CancellationToken cancellationToken) =>
        await _agent.PostAndReplyAsync(replyChannel => new OpenChannel(replyChannel, cancellationToken));

    async Task<Object> IAmqpClientAgent.Disconnect() =>
        await _agent.PostAndReplyAsync(replyChannel => new Disconnect(replyChannel));

    async Task IAmqpClientAgent.Stop() =>
        await _agent.StopAsync();
}
