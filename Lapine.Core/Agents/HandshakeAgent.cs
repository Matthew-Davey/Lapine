namespace Lapine.Agents;

using System.Reactive.Linq;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

using static System.Math;
using static System.Text.Encoding;
using static Lapine.Agents.DispatcherAgent.Protocol;
using static Lapine.Agents.HandshakeAgent.Protocol;
using static Lapine.Agents.SocketAgent.Protocol;

static class HandshakeAgent {
    static public class Protocol {
        public record StartHandshake(ConnectionConfiguration ConnectionConfiguration);
    }

    static public IAgent Create(IObservable<RawFrame> receivedFrames, IObservable<Object> connectionEvents, IAgent dispatcher, CancellationToken cancellationToken) =>
        Agent.StartNew(Unstarted(receivedFrames, connectionEvents, dispatcher, cancellationToken));

    static Behaviour Unstarted(IObservable<RawFrame> receivedFrames, IObservable<Object> connectionEvents, IAgent dispatcher, CancellationToken cancellationToken) =>
        async context => {
            switch (context.Message) {
                case Started: {
                    return context;
                }
                case (StartHandshake(var connectionConfiguration), AsyncReplyChannel replyChannel): {
                    var framesSubscription = receivedFrames
                        .Where(frame => frame.Channel == 0)
                        .Where(frame => frame.Type == FrameType.Method)
                        .Subscribe(frame => context.Self.PostAsync(RawFrame.UnwrapMethod(frame)));

                    var connectionEventsSubscription = connectionEvents.Subscribe(message => context.Self.PostAsync(message));

                    var scheduledTimeout = cancellationToken.Register(() => context.Self.PostAsync(new TimeoutException("A connection to the broker was established but the negotiation did not complete within the specified connection timeout limit.")));

                    await dispatcher.PostAsync(Dispatch.ProtocolHeader(ProtocolHeader.Default));

                    return context with {
                        Behaviour = AwaitingConnectionStart(connectionConfiguration, scheduledTimeout, framesSubscription, connectionEventsSubscription, dispatcher, replyChannel)
                    };
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Unstarted)}' behaviour.");
            }
        };

    static Behaviour AwaitingConnectionStart(ConnectionConfiguration connectionConfiguration, CancellationTokenRegistration scheduledTimeout, IDisposable frameSubscription, IDisposable connectionEventsSubscription, IAgent dispatcher, AsyncReplyChannel replyChannel) =>
        async context => {
            switch (context.Message) {
                case ConnectionStart message when !message.Mechanisms.Contains(connectionConfiguration.AuthenticationStrategy.Mechanism): {
                    await scheduledTimeout.DisposeAsync();
                    replyChannel.Reply(new Exception($"Requested authentication mechanism '{connectionConfiguration.AuthenticationStrategy.Mechanism}' is not supported by the broker. This broker supports {String.Join(", ", message.Mechanisms)}"));
                    context.Self.StopAsync();
                    frameSubscription.Dispose();
                    connectionEventsSubscription.Dispose();
                    return context;
                }
                case ConnectionStart(var version, var serverProperties, var mechanisms, var locales) when !locales.Contains(connectionConfiguration.Locale): {
                    await scheduledTimeout.DisposeAsync();
                    replyChannel.Reply(new Exception($"Requested locale '{connectionConfiguration.Locale}' is not supported by the broker. This broker supports {String.Join(", ", locales)}"));
                    context.Self.StopAsync();
                    frameSubscription.Dispose();
                    connectionEventsSubscription.Dispose();
                    return context;
                }
                case TimeoutException timeout: {
                    replyChannel.Reply(timeout);
                    context.Self.StopAsync();
                    frameSubscription.Dispose();
                    connectionEventsSubscription.Dispose();
                    return context;
                }
                case ConnectionStart(var version, var serverProperties, var mechanisms, var locales): {
                    var authenticationResponse = connectionConfiguration.AuthenticationStrategy.Respond(
                        stage    : 0,
                        challenge: Span<Byte>.Empty
                    );
                    await dispatcher.PostAsync(Dispatch.Command(new ConnectionStartOk(
                        PeerProperties: connectionConfiguration.PeerProperties.ToDictionary(),
                        Mechanism     : connectionConfiguration.AuthenticationStrategy.Mechanism,
                        Response      : UTF8.GetString(authenticationResponse),
                        Locale        : connectionConfiguration.Locale
                    )));
                    return context with {
                        Behaviour = AwaitingConnectionSecureOrTune(connectionConfiguration, scheduledTimeout, frameSubscription, connectionEventsSubscription, 0, serverProperties, dispatcher, replyChannel)
                    };
                }
                case Stopped: {
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingConnectionStart)}' behaviour.");
            }
        };

    static Behaviour AwaitingConnectionSecureOrTune(ConnectionConfiguration connectionConfiguration, CancellationTokenRegistration scheduledTimeout, IDisposable frameSubscription, IDisposable connectionEventsSubscription, Byte authenticationStage, IReadOnlyDictionary<String, Object> serverProperties, IAgent dispatcher, AsyncReplyChannel replyChannel) =>
        async context => {
            switch (context.Message) {
                case RemoteDisconnected(var fault): {
                    replyChannel.Reply(fault);
                    context.Self.StopAsync();
                    frameSubscription.Dispose();
                    connectionEventsSubscription.Dispose();
                    return context;
                }
                case TimeoutException timeout: {
                    replyChannel.Reply(timeout);
                    context.Self.StopAsync();
                    frameSubscription.Dispose();
                    connectionEventsSubscription.Dispose();
                    return context;
                }
                case ConnectionSecure(var challenge): {
                    var challengeBytes = UTF8.GetBytes(challenge);
                    var authenticationResponse = connectionConfiguration.AuthenticationStrategy.Respond(
                        stage    : ++authenticationStage,
                        challenge: challengeBytes
                    );
                    await dispatcher.PostAsync(Dispatch.Command(new ConnectionSecureOk(
                        Response: UTF8.GetString(authenticationResponse)
                    )));
                    return context with {
                        Behaviour = AwaitingConnectionSecureOrTune(connectionConfiguration, scheduledTimeout, frameSubscription, connectionEventsSubscription, authenticationStage, serverProperties, dispatcher, replyChannel)
                    };
                }
                case ConnectionTune(var channelMax, var frameMax, var heartbeat): {
                    var heartbeatFrequency = Min(heartbeat, (UInt16)connectionConfiguration.ConnectionIntegrityStrategy.HeartbeatFrequency.GetValueOrDefault().TotalSeconds);
                    var maxFrameSize       = Min(frameMax, connectionConfiguration.MaximumFrameSize);
                    var maxChannelCount    = Min(channelMax, connectionConfiguration.MaximumChannelCount);

                    await dispatcher.PostAsync(Dispatch.Command(new ConnectionTuneOk(
                        ChannelMax: maxChannelCount,
                        FrameMax  : maxFrameSize,
                        Heartbeat : heartbeatFrequency
                    )));
                    await dispatcher.PostAsync(Dispatch.Command(new ConnectionOpen(
                        VirtualHost: connectionConfiguration.VirtualHost
                    )));
                    return context with {
                        Behaviour = AwaitingConnectionOpenOk(scheduledTimeout, frameSubscription, connectionEventsSubscription, maxChannelCount, maxFrameSize, heartbeatFrequency, serverProperties, replyChannel)
                    };
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingConnectionSecureOrTune)}' behaviour.");
            }
        };

    static Behaviour AwaitingConnectionOpenOk(CancellationTokenRegistration scheduledTimeout, IDisposable frameSubscription, IDisposable connectionEventSubscription, UInt16 maxChannelCount, UInt32 maxFrameSize, UInt16 heartbeatFrequency, IReadOnlyDictionary<String, Object> serverProperties, AsyncReplyChannel replyChannel) =>
        async context => {
            switch (context.Message) {
                case TimeoutException timeout: {
                    replyChannel.Reply(timeout);
                    context.Self.StopAsync();
                    frameSubscription.Dispose();
                    connectionEventSubscription.Dispose();
                    return context;
                }
                case ConnectionOpenOk: {
                    await scheduledTimeout.DisposeAsync();
                    replyChannel.Reply(new ConnectionAgreement(
                        MaxChannelCount   : maxChannelCount,
                        MaxFrameSize      : maxFrameSize,
                        HeartbeatFrequency: TimeSpan.FromSeconds(heartbeatFrequency),
                        ServerProperties  : serverProperties
                    ));
                    context.Self.StopAsync();
                    frameSubscription.Dispose();
                    connectionEventSubscription.Dispose();
                    return context;
                }
                case Stopped: {
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingConnectionOpenOk)}' behaviour.");
            }
        };
}
