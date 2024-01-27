namespace Lapine.Agents;

using System.Reactive.Linq;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

using static System.Math;
using static System.Text.Encoding;

interface IHandshakeAgent {
    Task<HandshakeAgent.HandshakeResult> StartHandshake(ConnectionConfiguration connectionConfiguration);
}

class HandshakeAgent : IHandshakeAgent {
    readonly IAgent<Protocol> _agent;

    HandshakeAgent(IAgent<Protocol> agent) =>
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));

    public abstract record HandshakeResult;
    public record ConnectionAgreed(ConnectionAgreement Agreement) : HandshakeResult;
    public record HandshakeFailed(Exception Fault) : HandshakeResult;

    abstract record Protocol;
    record StartHandshake(ConnectionConfiguration ConnectionConfiguration, AsyncReplyChannel ReplyChannel) : Protocol;
    record FrameReceived(ICommand Frame) : Protocol;
    record Timeout(TimeoutException Exception) : Protocol;
    record ConnectionEventReceived(Object Message) : Protocol;

    static public IHandshakeAgent Create(IObservable<RawFrame> receivedFrames, IObservable<Object> connectionEvents, IDispatcherAgent dispatcher, CancellationToken cancellationToken) =>
        new HandshakeAgent(Agent<Protocol>.StartNew(Unstarted(receivedFrames, connectionEvents, dispatcher, cancellationToken)));

    static Behaviour<Protocol> Unstarted(IObservable<RawFrame> receivedFrames, IObservable<Object> connectionEvents, IDispatcherAgent dispatcher, CancellationToken cancellationToken) =>
        async context => {
            switch (context.Message) {
                case StartHandshake(var connectionConfiguration, var replyChannel): {
                    var framesSubscription = receivedFrames
                        .Where(frame => frame.Channel == 0)
                        .Where(frame => frame.Type == FrameType.Method)
                        .Subscribe(frame => context.Self.PostAsync(new FrameReceived(RawFrame.UnwrapMethod(frame))));

                    var connectionEventsSubscription = connectionEvents.Subscribe(message => context.Self.PostAsync(new ConnectionEventReceived(message)));

                    var scheduledTimeout = cancellationToken.Register(() => context.Self.PostAsync(new Timeout(new TimeoutException("A connection to the broker was established but the negotiation did not complete within the specified connection timeout limit."))));

                    await dispatcher.Dispatch(ProtocolHeader.Default);

                    return context with {
                        Behaviour = AwaitingConnectionStart(connectionConfiguration, scheduledTimeout, framesSubscription, connectionEventsSubscription, dispatcher, replyChannel)
                    };
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Unstarted)}' behaviour.");
            }
        };

    static Behaviour<Protocol> AwaitingConnectionStart(ConnectionConfiguration connectionConfiguration, CancellationTokenRegistration scheduledTimeout, IDisposable frameSubscription, IDisposable connectionEventsSubscription, IDispatcherAgent dispatcher, AsyncReplyChannel replyChannel) =>
        async context => {
            switch (context.Message) {
                case FrameReceived(ConnectionStart message) when !message.Mechanisms.Contains(connectionConfiguration.AuthenticationStrategy.Mechanism): {
                    await scheduledTimeout.DisposeAsync();
                    replyChannel.Reply(new HandshakeFailed(new Exception($"Requested authentication mechanism '{connectionConfiguration.AuthenticationStrategy.Mechanism}' is not supported by the broker. This broker supports {String.Join(", ", message.Mechanisms)}")));
                    await context.Self.StopAsync();
                    frameSubscription.Dispose();
                    connectionEventsSubscription.Dispose();
                    return context;
                }
                case FrameReceived(ConnectionStart(var version, var serverProperties, var mechanisms, var locales)) when !locales.Contains(connectionConfiguration.Locale): {
                    await scheduledTimeout.DisposeAsync();
                    replyChannel.Reply(new HandshakeFailed(new Exception($"Requested locale '{connectionConfiguration.Locale}' is not supported by the broker. This broker supports {String.Join(", ", locales)}")));
                    await context.Self.StopAsync();
                    frameSubscription.Dispose();
                    connectionEventsSubscription.Dispose();
                    return context;
                }
                case Timeout(var exception): {
                    replyChannel.Reply(new HandshakeFailed(exception));
                    await context.Self.StopAsync();
                    frameSubscription.Dispose();
                    connectionEventsSubscription.Dispose();
                    return context;
                }
                case FrameReceived(ConnectionStart(var version, var serverProperties, var mechanisms, var locales)): {
                    var authenticationResponse = connectionConfiguration.AuthenticationStrategy.Respond(
                        stage    : 0,
                        challenge: Span<Byte>.Empty
                    );
                    await dispatcher.Dispatch(new ConnectionStartOk(
                        PeerProperties: connectionConfiguration.PeerProperties.ToDictionary(),
                        Mechanism     : connectionConfiguration.AuthenticationStrategy.Mechanism,
                        Response      : UTF8.GetString(authenticationResponse),
                        Locale        : connectionConfiguration.Locale
                    ));
                    return context with {
                        Behaviour = AwaitingConnectionSecureOrTune(connectionConfiguration, scheduledTimeout, frameSubscription, connectionEventsSubscription, 0, serverProperties, dispatcher, replyChannel)
                    };
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingConnectionStart)}' behaviour.");
            }
        };

    static Behaviour<Protocol> AwaitingConnectionSecureOrTune(ConnectionConfiguration connectionConfiguration, CancellationTokenRegistration scheduledTimeout, IDisposable frameSubscription, IDisposable connectionEventsSubscription, Byte authenticationStage, IReadOnlyDictionary<String, Object> serverProperties, IDispatcherAgent dispatcher, AsyncReplyChannel replyChannel) =>
        async context => {
            switch (context.Message) {
                case ConnectionEventReceived(RemoteDisconnected(var fault)): {
                    replyChannel.Reply(new HandshakeFailed(fault));
                    await context.Self.StopAsync();
                    frameSubscription.Dispose();
                    connectionEventsSubscription.Dispose();
                    return context;
                }
                case Timeout(var exception): {
                    replyChannel.Reply(new HandshakeFailed(exception));
                    await context.Self.StopAsync();
                    frameSubscription.Dispose();
                    connectionEventsSubscription.Dispose();
                    return context;
                }
                case FrameReceived(ConnectionSecure(var challenge)): {
                    var challengeBytes = UTF8.GetBytes(challenge);
                    var authenticationResponse = connectionConfiguration.AuthenticationStrategy.Respond(
                        stage    : ++authenticationStage,
                        challenge: challengeBytes
                    );
                    await dispatcher.Dispatch(new ConnectionSecureOk(
                        Response: UTF8.GetString(authenticationResponse)
                    ));
                    return context with {
                        Behaviour = AwaitingConnectionSecureOrTune(connectionConfiguration, scheduledTimeout, frameSubscription, connectionEventsSubscription, authenticationStage, serverProperties, dispatcher, replyChannel)
                    };
                }
                case FrameReceived(ConnectionTune(var channelMax, var frameMax, var heartbeat)): {
                    var heartbeatFrequency = Min(heartbeat, (UInt16)connectionConfiguration.ConnectionIntegrityStrategy.HeartbeatFrequency.GetValueOrDefault().TotalSeconds);
                    var maxFrameSize       = Min(frameMax, connectionConfiguration.MaximumFrameSize);
                    var maxChannelCount    = Min(channelMax, connectionConfiguration.MaximumChannelCount);

                    await dispatcher.Dispatch(new ConnectionTuneOk(
                        ChannelMax: maxChannelCount,
                        FrameMax  : maxFrameSize,
                        Heartbeat : heartbeatFrequency
                    ));
                    await dispatcher.Dispatch(new ConnectionOpen(
                        VirtualHost: connectionConfiguration.VirtualHost
                    ));
                    return context with {
                        Behaviour = AwaitingConnectionOpenOk(scheduledTimeout, frameSubscription, connectionEventsSubscription, maxChannelCount, maxFrameSize, heartbeatFrequency, serverProperties, replyChannel)
                    };
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingConnectionSecureOrTune)}' behaviour.");
            }
        };

    static Behaviour<Protocol> AwaitingConnectionOpenOk(CancellationTokenRegistration scheduledTimeout, IDisposable frameSubscription, IDisposable connectionEventSubscription, UInt16 maxChannelCount, UInt32 maxFrameSize, UInt16 heartbeatFrequency, IReadOnlyDictionary<String, Object> serverProperties, AsyncReplyChannel replyChannel) =>
        async context => {
            switch (context.Message) {
                case Timeout(var exception): {
                    replyChannel.Reply(new HandshakeFailed(exception));
                    await context.Self.StopAsync();
                    frameSubscription.Dispose();
                    connectionEventSubscription.Dispose();
                    return context;
                }
                case FrameReceived(ConnectionOpenOk): {
                    await scheduledTimeout.DisposeAsync();
                    replyChannel.Reply(new ConnectionAgreed(new ConnectionAgreement(
                        MaxChannelCount   : maxChannelCount,
                        MaxFrameSize      : maxFrameSize,
                        HeartbeatFrequency: TimeSpan.FromSeconds(heartbeatFrequency),
                        ServerProperties  : serverProperties
                    )));
                    await context.Self.StopAsync();
                    frameSubscription.Dispose();
                    connectionEventSubscription.Dispose();
                    return context;
                }
                default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(AwaitingConnectionOpenOk)}' behaviour.");
            }
        };

    async Task<HandshakeResult> IHandshakeAgent.StartHandshake(ConnectionConfiguration connectionConfiguration) {
        var reply = await _agent.PostAndReplyAsync(replyChannel => new StartHandshake(connectionConfiguration, replyChannel));
        return (HandshakeResult) reply;
    }
}
