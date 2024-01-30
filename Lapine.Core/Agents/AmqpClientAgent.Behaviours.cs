namespace Lapine.Agents;

using System.Collections.Immutable;
using System.Net;
using System.Reactive.Linq;
using Lapine.Client;
using Lapine.Protocol;

static partial class AmqpClientAgent {
    record State(
        ConnectionConfiguration ConnectionConfiguration,
        ISocketAgent SocketAgent,
        IHeartbeatAgent HeartbeatAgent,
        IObservable<RawFrame> ReceivedFrames,
        IObservable<ConnectionEvent> ConnectionEvents,
        IDispatcherAgent Dispatcher,
        IImmutableList<UInt16> AvailableChannelIds
    );

    static Behaviour<Protocol> Disconnected() =>
        async context => {
            switch (context.Message) {
                case EstablishConnection(var connectionConfiguration, var replyChannel, var cancellationToken): {
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(connectionConfiguration.ConnectionTimeout);

                    var remainingEndpoints = new Queue<IPEndPoint>(connectionConfiguration.GetConnectionSequence());

                    if (remainingEndpoints.Count == 0) {
                        replyChannel.Fault(new Exception("No endpoints specified in connection configuration"));
                        return context;
                    }

                    var accumulatedFailures = new List<Exception>();

                    while (remainingEndpoints.Any()) {
                        var endpoint = remainingEndpoints.Dequeue();
                        var socketAgent = SocketAgent.Create();

                        try {
                            var (connectionEvents, receivedFrames) = await socketAgent.ConnectAsync(endpoint, cts.Token);

                            var dispatcher = DispatcherAgent.Create();
                            await dispatcher.DispatchTo(socketAgent, 0);

                            var handshakeAgent = HandshakeAgent.Create(
                                receivedFrames   : receivedFrames,
                                connectionEvents : connectionEvents,
                                dispatcher       : dispatcher,
                                cancellationToken: cts.Token
                            );

                            var connectionAgreement = await handshakeAgent.StartHandshake(connectionConfiguration);
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

                            replyChannel.Complete();

                            var state = new State(
                                ConnectionConfiguration: connectionConfiguration,
                                SocketAgent            : socketAgent,
                                HeartbeatAgent         : heartbeatAgent,
                                ReceivedFrames         : receivedFrames,
                                ConnectionEvents       : connectionEvents,
                                Dispatcher             : dispatcher,
                                AvailableChannelIds    : Enumerable.Range(1, connectionAgreement.MaxChannelCount)
                                    .Select(channelId => (UInt16)channelId)
                                    .ToImmutableList()
                            );

                            return context with {
                                Behaviour = Connected(state)
                            };
                        }
                        catch (Exception fault) {
                            accumulatedFailures.Add(fault);
                        }
                    }
                    replyChannel.Fault(new AggregateException("Could not connect to any of the configured endpoints", accumulatedFailures));
                    return context;
                }
                case Disconnect(var replyChannel): {
                    replyChannel.Complete();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Disconnected)}' behaviour.");
            }
        };

    static Behaviour<Protocol> Connected(State state) =>
        async context => {
            switch (context.Message) {
                case HeartbeatEventEventReceived(RemoteFlatline): {
                    await state.HeartbeatAgent.Stop();
                    await state.Dispatcher.Stop();
                    await state.SocketAgent.Disconnect();
                    await state.SocketAgent.StopAsync();

                    return context with { Behaviour = Disconnected() };
                }
                case Disconnect(var replyChannel): {
                    await state.HeartbeatAgent.Stop();
                    await state.Dispatcher.Stop();
                    await state.SocketAgent.Disconnect();
                    await state.SocketAgent.StopAsync();

                    replyChannel.Complete();

                    return context;
                }
                case OpenChannel(var replyChannel, var cancellationToken): {
                    var channelId = state.AvailableChannelIds[0];
                    var channelAgent = ChannelAgent.Create(state.ConnectionConfiguration.MaximumFrameSize);

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(state.ConnectionConfiguration.CommandTimeout);

                    return await channelAgent.Open(channelId, state.ReceivedFrames.Where(frame => frame.Channel == channelId), state.ConnectionEvents, state.SocketAgent, cts.Token)
                        .ContinueWith(
                            onCompleted: () => {
                                replyChannel.Reply(channelAgent);
                                return context with {
                                    Behaviour = Connected(state with { AvailableChannelIds = state.AvailableChannelIds.Remove(channelId) })
                                };
                            },
                            onFaulted: fault => {
                                replyChannel.Fault(fault);
                                return context;
                            }
                        );
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Connected)}' behaviour.");
            }
        };
}
