namespace Lapine.Agents;

using System.Collections.Immutable;
using System.Net;
using System.Reactive.Linq;
using Lapine.Client;
using Lapine.Protocol;

using static Lapine.Agents.ChannelAgent.Protocol;
using static Lapine.Agents.DispatcherAgent.Protocol;
using static Lapine.Agents.HandshakeAgent.Protocol;
using static Lapine.Agents.HeartbeatAgent.Protocol;
using static Lapine.Agents.AmqpClientAgent.Protocol;
using static Lapine.Agents.SocketAgent.Protocol;

static class AmqpClientAgent {
    static public class Protocol {
        public record EstablishConnection(ConnectionConfiguration Configuration, CancellationToken CancellationToken = default);
        public record OpenChannel(CancellationToken CancellationToken = default);
        public record Disconnect;
    }

    static public IAgent Create() =>
        Agent.StartNew(Disconnected());

    static Behaviour Disconnected() =>
        async context => {
            switch (context.Message) {
                case (EstablishConnection(var connectionConfiguration, var cancellationToken), AsyncReplyChannel replyChannel): {
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

                        switch (await socketAgent.PostAndReplyAsync(new Connect(endpoint, cts.Token))) {
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
                                await dispatcher.PostAsync(new DispatchTo(socketAgent, 0));

                                var handshakeAgent = HandshakeAgent.Create(
                                    receivedFrames   : receivedFrames,
                                    connectionEvents : connectionEvents,
                                    dispatcher       : dispatcher,
                                    cancellationToken: cts.Token
                                );

                                switch (await handshakeAgent.PostAndReplyAsync(new StartHandshake(connectionConfiguration))) {
                                    case ConnectionAgreement connectionAgreement: {
                                        await socketAgent.PostAsync(new Tune(connectionAgreement.MaxFrameSize));

                                        var heartbeatAgent = HeartbeatAgent.Create();

                                        if (connectionConfiguration.ConnectionIntegrityStrategy.HeartbeatFrequency.HasValue) {
                                            var heartbeatEvents = (IObservable<Object>) await heartbeatAgent.PostAndReplyAsync(new StartHeartbeat(
                                                ReceivedFrames: receivedFrames,
                                                Dispatcher    : dispatcher,
                                                Frequency     : connectionConfiguration.ConnectionIntegrityStrategy.HeartbeatFrequency.Value
                                            ));
                                            heartbeatEvents.Subscribe(onNext: message => context.Self.PostAndReplyAsync(message));
                                        }

                                        // If tcp keepalives are enabled, configure the socket...
                                        if (connectionConfiguration.ConnectionIntegrityStrategy.KeepAliveSettings.HasValue) {
                                            var (probeTime, retryInterval, retryCount) = connectionConfiguration.ConnectionIntegrityStrategy.KeepAliveSettings.Value;

                                            await socketAgent.PostAsync(new EnableTcpKeepAlives(
                                                ProbeTime    : probeTime,
                                                RetryInterval: retryInterval,
                                                RetryCount   : retryCount
                                            ), cts.Token);
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
                                    case Exception fault: {
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
                case (Protocol.Disconnect, AsyncReplyChannel replyChannel): {
                    replyChannel.Reply(true);
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Disconnected)}' behaviour.");
            }
        };

    static Behaviour Connected(ConnectionConfiguration connectionConfiguration, IAgent socketAgent, IAgent heartbeatAgent, IObservable<RawFrame> receivedFrames, IObservable<Object> connectionEvents, IAgent dispatcher, IImmutableList<UInt16> availableChannelIds) =>
        async context => {
            switch (context.Message) {
                case RemoteFlatline: {
                    await heartbeatAgent.StopAsync();
                    await dispatcher.StopAsync();
                    await socketAgent.PostAsync(new SocketAgent.Protocol.Disconnect());
                    await socketAgent.StopAsync();

                    return context with { Behaviour = Disconnected() };
                }
                case (AmqpClientAgent.Protocol.Disconnect, AsyncReplyChannel replyChannel): {
                    await heartbeatAgent.StopAsync();
                    await dispatcher.StopAsync();
                    await socketAgent.PostAsync(new SocketAgent.Protocol.Disconnect());
                    await socketAgent.StopAsync();

                    replyChannel.Reply(true);

                    return context;
                }
                case (OpenChannel(var cancellationToken), AsyncReplyChannel replyChannel): {
                    var channelId = availableChannelIds[0];
                    var channelAgent = ChannelAgent.Create(connectionConfiguration.MaximumFrameSize);

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(connectionConfiguration.CommandTimeout);

                    var command = new Open(
                        ChannelId        : channelId,
                        ReceivedFrames   : receivedFrames.Where(frame => frame.Channel == channelId),
                        ConnectionEvents : connectionEvents,
                        SocketAgent      : socketAgent,
                        CancellationToken: cts.Token
                    );

                    switch (await channelAgent.PostAndReplyAsync(command)) {
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
}
