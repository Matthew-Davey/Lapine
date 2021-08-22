namespace Lapine.Agents;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lapine.Agents.ProcessManagers;
using Lapine.Client;
using Lapine.Protocol;
using Proto;
using Proto.Timers;

using static System.Threading.Tasks.Task;
using static Lapine.Agents.ChannelAgent.Protocol;
using static Lapine.Agents.DispatcherAgent.Protocol;
using static Lapine.Agents.HeartbeatAgent.Protocol;
using static Lapine.Agents.RabbitClientAgent.Protocol;
using static Lapine.Agents.SocketAgent.Protocol;

using TimeoutExpired = Lapine.Agents.RabbitClientAgent.Protocol.TimeoutExpired;

static class RabbitClientAgent {
    static public class Protocol {
        public record EstablishConnection(ConnectionConfiguration Configuration) : AsyncCommand;
        public record OpenChannel(TimeSpan Timeout) : AsyncCommand<PID>;
        internal record TimeoutExpired;
    }

    readonly record struct ConnectingState(
        ConnectionConfiguration ConnectionConfiguration,
        PID SocketAgent,
        IPEndPoint[] RemainingEndpoints,
        CancellationTokenSource ScheduledTimeout,
        IImmutableList<Exception> AccumulatedFailures
    );

    readonly record struct ConnectedState(
        ConnectionConfiguration ConnectionConfiguration,
        PID SocketAgent,
        PID TxD,
        PID Dispatcher,
        IImmutableList<UInt16> AvailableChannelIds
    );

    static public Props Create() =>
        Props.FromProducer(() => new Actor());

    class Actor : IActor {
        readonly Behavior _behaviour;

        public Actor() =>
            _behaviour = new Behavior(Disconnected);

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Disconnected(IContext context) {
            switch (context.Message) {
                case EstablishConnection connect: {
                    var state = new ConnectingState(
                        ConnectionConfiguration: connect.Configuration,
                        SocketAgent: context.SpawnNamed(
                            name: "socket",
                            props: SocketAgent.Create()
                        ),
                        RemainingEndpoints: connect.Configuration.GetConnectionSequence(),
                        ScheduledTimeout: context.Scheduler().SendOnce(
                            delay  : connect.Configuration.ConnectionTimeout,
                            target : context.Self!,
                            message: new TimeoutExpired()
                        ),
                        AccumulatedFailures: ImmutableList<Exception>.Empty
                    );
                    if (state.RemainingEndpoints.Length > 0) {
                        context.Send(state.SocketAgent, new Connect(
                            Endpoint      : state.RemainingEndpoints[0],
                            ConnectTimeout: connect.Configuration.ConnectionTimeout,
                            Listener      : context.Self!
                        ));
                        _behaviour.Become(Connecting(connect, state with {
                            RemainingEndpoints = state.RemainingEndpoints[1..]
                        }));
                    }
                    else {
                        connect.SetException(new Exception("No endpoints specified in connection configuration"));
                        _behaviour.Become(Disconnected);
                    }
                    break;
                }
            }
            return CompletedTask;
        }

        Receive Connecting(EstablishConnection command, ConnectingState state) =>
            async (IContext context) => {
                switch (context.Message) {
                    case ConnectionFailed failed when state.RemainingEndpoints.Length > 0: {
                        context.Send(state.SocketAgent, new Connect(
                            Endpoint      : state.RemainingEndpoints[0],
                            ConnectTimeout: state.ConnectionConfiguration!.ConnectionTimeout,
                            Listener      : context.Self!
                        ));
                        _behaviour.Become(Connecting(command, state with {
                            RemainingEndpoints  = state.RemainingEndpoints[1..],
                            AccumulatedFailures = state.AccumulatedFailures.Add(failed.Reason)
                        }));
                        break;
                    }
                    case ConnectionFailed failed when state.RemainingEndpoints.Length == 0: {
                        state.ScheduledTimeout.Cancel();
                        command.SetException(new AggregateException("Could not connect to any of the configured endpoints", state.AccumulatedFailures.Add(failed.Reason)));
                        context.Stop(state.SocketAgent);
                        _behaviour.Become(Disconnected);
                        break;
                    }
                    case Connected connected: {
                        var dispatcher = context.SpawnNamed(
                            name : "dispatcher",
                            props: DispatcherAgent.Create()
                        );

                        context.Send(connected.RxD, new BeginPolling());
                        context.Send(dispatcher, new DispatchTo(connected.TxD, 0));

                        var promise = new TaskCompletionSource<ConnectionAgreement>();
                        context.Spawn(HandshakeProcessManager.Create(
                            connectionConfiguration: state.ConnectionConfiguration,
                            dispatcher             : dispatcher,
                            timeout                : state.ConnectionConfiguration.ConnectionTimeout,
                            promise                : promise
                        ));
                        await promise.Task.ContinueWith(
                            onCompleted: (connectionAgreement) => {
                                state.ScheduledTimeout.Cancel();
                                context.Send(state.SocketAgent, new Tune(connectionAgreement.MaxFrameSize));

                                // If amqp heartbeats are enabled, spawn a heartbeat agent...
                                if (state.ConnectionConfiguration.ConnectionIntegrityStrategy.HeartbeatFrequency.HasValue) {
                                    var heartbeatAgent = context.SpawnNamed(
                                        name: "heartbeat",
                                        props: HeartbeatAgent.Create()
                                    );
                                    context.Send(heartbeatAgent, new StartHeartbeat(
                                        Dispatcher: dispatcher,
                                        Frequency : state.ConnectionConfiguration.ConnectionIntegrityStrategy.HeartbeatFrequency.Value,
                                        Listener  : context.Self!
                                    ));
                                }

                                // If tcp keepalives are enabled, configure the socket...
                                if (state.ConnectionConfiguration.ConnectionIntegrityStrategy.KeepAliveSettings.HasValue) {
                                    var (probeTime, retryInterval, retryCount) = state.ConnectionConfiguration.ConnectionIntegrityStrategy.KeepAliveSettings.Value;

                                    context.Send(state.SocketAgent, new EnableTcpKeepAlives(
                                        ProbeTime    : probeTime,
                                        RetryInterval: retryInterval,
                                        RetryCount   : retryCount
                                    ));
                                }

                                _behaviour.Become(Connected(new ConnectedState(
                                    ConnectionConfiguration: state.ConnectionConfiguration,
                                    SocketAgent            : state.SocketAgent,
                                    TxD                    : connected.TxD,
                                    Dispatcher             : dispatcher,
                                    AvailableChannelIds    : Enumerable.Range(1, connectionAgreement.MaxChannelCount)
                                        .Select(channelId => (UInt16)channelId)
                                        .ToImmutableList()
                                )));

                                command.SetResult();
                            },
                            onFaulted: (fault) => {
                                state.ScheduledTimeout.Cancel();
                                command.SetException(fault);
                                _behaviour.Become(Disconnected);
                            }
                        );
                        break;
                    }
                    case TimeoutExpired _: {
                        command.SetException(new TimeoutException("Unable to connect to any of the configured endpoints within the connection timeout limit"));
                        context.Stop(state.SocketAgent);
                        _behaviour.Become(Disconnected);
                        break;
                    }
                }
            };

        Receive Connected(ConnectedState state) =>
            async (IContext context) => {
                switch (context.Message) {
                    case RemoteFlatline _: {
                        context.Stop(state.Dispatcher);
                        context.Stop(state.SocketAgent);

                        _behaviour.Become(Disconnected);
                        break;
                    }
                    case OpenChannel openChannel: {
                        var channelId = state.AvailableChannelIds[0];
                        var channelAgent = context.SpawnNamed(
                            name: $"channel_{channelId}",
                            props: ChannelAgent.Create(state.ConnectionConfiguration.MaximumFrameSize)
                        );
                        var command = new Open(channelId, state.TxD!, openChannel.Timeout);
                        context.Send(channelAgent, command);

                        try {
                            await command;
                            _behaviour.Become(Connected(state with {
                                AvailableChannelIds = state.AvailableChannelIds.Remove(channelId)
                            }));
                            openChannel.SetResult(channelAgent);
                        }
                        catch (Exception fault) {
                            openChannel.SetException(fault);
                        }
                        break;
                    };
                }
            };
    }
}
