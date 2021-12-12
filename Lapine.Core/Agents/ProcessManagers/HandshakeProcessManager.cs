namespace Lapine.Agents.ProcessManagers;

using Lapine.Agents.Middleware;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;
using Proto;
using Proto.Timers;

using static System.Math;
using static System.Text.Encoding;
using static System.Threading.Tasks.Task;
using static Lapine.Agents.DispatcherAgent.Protocol;
using static Lapine.Agents.SocketAgent.Protocol;

class HandshakeProcessManager : IActor {
    readonly Behavior _behaviour;
    readonly ConnectionConfiguration _connectionConfiguration;
    readonly PID _dispatcher;
    readonly TimeSpan _timeout;
    readonly TaskCompletionSource<ConnectionAgreement> _promise;

    public HandshakeProcessManager(ConnectionConfiguration connectionConfiguration, PID dispatcher, TimeSpan timeout, TaskCompletionSource<ConnectionAgreement> promise) {
        _behaviour               = new Behavior(Unstarted);
        _connectionConfiguration = connectionConfiguration;
        _dispatcher              = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _timeout                 = timeout;
        _promise                 = promise ?? throw new ArgumentNullException(nameof(promise));
    }

    static public Props Create(ConnectionConfiguration connectionConfiguration, PID dispatcher, TimeSpan timeout, TaskCompletionSource<ConnectionAgreement> promise) =>
        Props.FromProducer(() => new HandshakeProcessManager(connectionConfiguration, dispatcher, timeout, promise))
            .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames());

    public Task ReceiveAsync(IContext context) =>
        _behaviour.ReceiveAsync(context);

    Task Unstarted(IContext context) {
        switch (context.Message) {
            case Started: {
                var subscription = context.System.EventStream.Subscribe<FrameReceived>(
                    predicate: message => message.Frame.Channel == 0,
                    action   : message => context.Send(context.Self!, message)
                );
                var scheduledTimeout = context.Scheduler().SendOnce(_timeout, context.Self!, new TimeoutException("A connection to the broker was established but the negotiation did not complete within the specified connection timeout limit."));
                context.Send(_dispatcher, Dispatch.ProtocolHeader(ProtocolHeader.Default));
                _behaviour.Become(AwaitingConnectionStart(scheduledTimeout, subscription));
                break;
            }
        }
        return CompletedTask;
    }

    Receive AwaitingConnectionStart(CancellationTokenSource scheduledTimeout, EventStreamSubscription<Object> subscription) =>
        (IContext context) => {
            switch (context.Message) {
                case ConnectionStart message when !message.Mechanisms.Contains(_connectionConfiguration.AuthenticationStrategy.Mechanism): {
                    scheduledTimeout.Cancel();
                    _promise.SetException(new Exception($"Requested authentication mechanism '{_connectionConfiguration.AuthenticationStrategy.Mechanism}' is not supported by the broker. This broker supports {String.Join(", ", message.Mechanisms)}"));
                    context.Stop(context.Self!);
                    _behaviour.Become(Done(subscription));
                    break;
                }
                case ConnectionStart message when !message.Locales.Contains(_connectionConfiguration.Locale): {
                    scheduledTimeout.Cancel();
                    _promise.SetException(new Exception($"Requested locale '{_connectionConfiguration.Locale}' is not supported by the broker. This broker supports {String.Join(", ", message.Locales)}"));
                    context.Stop(context.Self!);
                    _behaviour.Become(Done(subscription));
                    break;
                }
                case TimeoutException timeout: {
                    _promise.SetException(timeout);
                    context.Stop(context.Self!);
                    _behaviour.Become(Done(subscription));
                    break;
                }
                case ConnectionStart message: {
                    var authenticationResponse = _connectionConfiguration.AuthenticationStrategy.Respond(
                        stage    : 0,
                        challenge: Span<Byte>.Empty
                    );
                    context.Send(_dispatcher, Dispatch.Command(new ConnectionStartOk(
                        PeerProperties: _connectionConfiguration.PeerProperties.ToDictionary(),
                        Mechanism     : _connectionConfiguration.AuthenticationStrategy.Mechanism,
                        Response      : UTF8.GetString(authenticationResponse),
                        Locale        : _connectionConfiguration.Locale
                    )));
                    _behaviour.Become(AwaitingConnectionSecureOrTune(scheduledTimeout, subscription, 0, message.ServerProperties));
                    break;
                }
            }
            return CompletedTask;
        };

    Receive AwaitingConnectionSecureOrTune(CancellationTokenSource scheduledTimeout, EventStreamSubscription<Object> subscription, Byte authenticationStage, IReadOnlyDictionary<String, Object> serverProperties) =>
        (IContext context) => {
            switch (context.Message) {
                case TimeoutException timeout: {
                    _promise.SetException(timeout);
                    context.Stop(context.Self!);
                    _behaviour.Become(Done(subscription));
                    break;
                }
                case ConnectionSecure message: {
                    var challenge = UTF8.GetBytes(message.Challenge);
                    var authenticationResponse = _connectionConfiguration.AuthenticationStrategy.Respond(
                        stage    : ++authenticationStage,
                        challenge: challenge
                    );
                    context.Send(_dispatcher, Dispatch.Command(new ConnectionSecureOk(
                        Response: UTF8.GetString(authenticationResponse)
                    )));
                    break;
                }
                case ConnectionTune tune: {
                    var heartbeatFrequency = Min(tune.Heartbeat, (UInt16)_connectionConfiguration.ConnectionIntegrityStrategy.HeartbeatFrequency.GetValueOrDefault().TotalSeconds);
                    var maxFrameSize       = Min(tune.FrameMax, _connectionConfiguration.MaximumFrameSize);
                    var maxChannelCount    = Min(tune.ChannelMax, _connectionConfiguration.MaximumChannelCount);

                    context.Send(_dispatcher, Dispatch.Command(new ConnectionTuneOk(
                        ChannelMax: maxChannelCount,
                        FrameMax  : maxFrameSize,
                        Heartbeat : heartbeatFrequency
                    )));
                    context.Send(_dispatcher, Dispatch.Command(new ConnectionOpen(
                        VirtualHost: _connectionConfiguration.VirtualHost
                    )));
                    _behaviour.Become(AwaitingConnectionOpenOk(scheduledTimeout, subscription, maxChannelCount, maxFrameSize, heartbeatFrequency, serverProperties));
                    break;
                }
            }
            return CompletedTask;
        };

    Receive AwaitingConnectionOpenOk(CancellationTokenSource scheduledTimeout, EventStreamSubscription<Object> subscription, UInt16 maxChannelCount, UInt32 maxFrameSize, UInt16 heartbeatFrequency, IReadOnlyDictionary<String, Object> serverProperties) =>
        (IContext context) => {
            switch (context.Message) {
                case TimeoutException timeout: {
                    _promise.SetException(timeout);
                    context.Stop(context.Self!);
                    _behaviour.Become(Done(subscription));
                    break;
                }
                case ConnectionOpenOk: {
                    scheduledTimeout.Cancel();
                    _promise.SetResult(new ConnectionAgreement(
                        MaxChannelCount   : maxChannelCount,
                        MaxFrameSize      : maxFrameSize,
                        HeartbeatFrequency: TimeSpan.FromSeconds(heartbeatFrequency),
                        ServerProperties  : serverProperties
                    ));
                    context.Stop(context.Self!);
                    _behaviour.Become(Done(subscription));
                    break;
                }
            }
            return CompletedTask;
        };

    static Receive Done(EventStreamSubscription<Object> subscription) =>
        (IContext context) => {
            switch (context.Message) {
                case Stopping: {
                    context.System.EventStream.Unsubscribe(subscription);
                    break;
                }
            }
            return CompletedTask;
        };
}
