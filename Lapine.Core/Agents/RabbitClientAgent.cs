namespace Lapine.Agents {
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Lapine.Client;
    using Proto;
    using Proto.Timers;

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.ChannelAgent.Protocol;
    using static Lapine.Agents.DispatcherAgent.Protocol;
    using static Lapine.Agents.FrameRouterAgent.Protocol;
    using static Lapine.Agents.HandshakeAgent.Protocol;
    using static Lapine.Agents.HeartbeatAgent.Protocol;
    using static Lapine.Agents.RabbitClientAgent.Protocol;
    using static Lapine.Agents.SocketAgent.Protocol;

    using TimeoutExpired = Lapine.Agents.RabbitClientAgent.Protocol.TimeoutExpired;

    static public class RabbitClientAgent {
        static public class Protocol {
            public record EstablishConnection(ConnectionConfiguration Configuration, TaskCompletionSource Promise);
            public record OpenChannel(TaskCompletionSource<PID> Promise);
            internal record TimeoutExpired();
        }

        record ConnectingState(
            ConnectionConfiguration ConnectionConfiguration,
            TaskCompletionSource Promise,
            PID SocketAgent,
            IPEndPoint[] RemainingEndpoints,
            CancellationTokenSource ScheduledTimeout,
            IImmutableList<Exception> AccumulatedFailures
        ) {
            public NegotiatingState ToNegotiatingState(PID txd, PID frameRouter, PID handshakeAgent, PID dispatcher) =>
                new (
                    ConnectionConfiguration: ConnectionConfiguration,
                    Promise                : Promise,
                    SocketAgent            : SocketAgent,
                    ScheduledTimeout       : ScheduledTimeout,
                    TxD                    : txd,
                    FrameRouter            : frameRouter,
                    HandshakeAgent         : handshakeAgent,
                    Dispatcher             : dispatcher
                );
        }

        record NegotiatingState(
            ConnectionConfiguration ConnectionConfiguration,
            TaskCompletionSource Promise,
            PID SocketAgent,
            CancellationTokenSource ScheduledTimeout,
            PID TxD,
            PID FrameRouter,
            PID HandshakeAgent,
            PID Dispatcher
        ) {
            public ConnectedState ToConnectedState(IImmutableList<UInt16> availableChannelIds) =>
                new (
                    ConnectionConfiguration: ConnectionConfiguration,
                    SocketAgent            : SocketAgent,
                    TxD                    : TxD,
                    FrameRouter            : FrameRouter,
                    Dispatcher             : Dispatcher,
                    AvailableChannelIds    : availableChannelIds
                );
        }

        record ConnectedState(
            ConnectionConfiguration ConnectionConfiguration,
            PID SocketAgent,
            PID TxD,
            PID FrameRouter,
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
                            Promise: connect.Promise,
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
                            _behaviour.Become(Connecting(state with {
                                RemainingEndpoints = state.RemainingEndpoints[1..]
                            }));
                        }
                        else {
                            connect.Promise.SetException(new Exception("No endpoints specified in connection configuration"));
                            _behaviour.Become(Disconnected);
                        }
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive Connecting(ConnectingState state) =>
                (IContext context) => {
                    switch (context.Message) {
                        case ConnectionFailed failed when state.RemainingEndpoints.Length > 0: {
                            context.Send(state.SocketAgent, new Connect(
                                Endpoint      : state.RemainingEndpoints[0],
                                ConnectTimeout: state.ConnectionConfiguration!.ConnectionTimeout,
                                Listener      : context.Self!
                            ));
                            _behaviour.Become(Connecting(state with {
                                RemainingEndpoints  = state.RemainingEndpoints[1..],
                                AccumulatedFailures = state.AccumulatedFailures.Add(failed.Reason)
                            }));
                            break;
                        }
                        case ConnectionFailed failed when state.RemainingEndpoints.Length == 0: {
                            state.ScheduledTimeout.Cancel();
                            state.Promise.SetException(new AggregateException("Could not connect to any of the configured endpoints", state.AccumulatedFailures.Add(failed.Reason)));
                            context.Stop(state.SocketAgent);
                            _behaviour.Become(Disconnected);
                            break;
                        }
                        case Connected connected: {
                            var negotiatingState = state.ToNegotiatingState(
                                txd : connected.TxD,
                                frameRouter: context.SpawnNamed(
                                    name : "frame_router",
                                    props: FrameRouterAgent.Create()
                                ),
                                handshakeAgent: context.SpawnNamed(
                                    name : "handshake",
                                    props: HandshakeAgent.Create()
                                ),
                                dispatcher: context.SpawnNamed(
                                    name : "dispatcher",
                                    props: DispatcherAgent.Create()
                                )
                            );

                            context.Send(connected.RxD, new BeginPolling(negotiatingState.FrameRouter));
                            context.Send(negotiatingState.FrameRouter, new AddRoutee(0, negotiatingState.HandshakeAgent));
                            context.Send(negotiatingState.Dispatcher, new DispatchTo(connected.TxD, 0));
                            context.Send(negotiatingState.HandshakeAgent, new BeginHandshake(
                                ConnectionConfiguration: state.ConnectionConfiguration!,
                                Listener               : context.Self!,
                                Dispatcher             : negotiatingState.Dispatcher
                            ));
                            _behaviour.Become(Negotiating(negotiatingState));
                            break;
                        }
                        case TimeoutExpired _: {
                            state.Promise.SetException(new TimeoutException("Unable to connect to any of the configured endpoints within the connection timeout limit"));
                            context.Stop(state.SocketAgent);
                            _behaviour.Become(Disconnected);
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive Negotiating(NegotiatingState state) =>
                (IContext context) => {
                    switch (context.Message) {
                        case HandshakeCompleted completed: {
                            state.ScheduledTimeout.Cancel();
                            context.Send(state.FrameRouter, new RemoveRoutee(0, state.HandshakeAgent));
                            context.Stop(state.HandshakeAgent);
                            context.Send(state.SocketAgent, new Tune(completed.MaxFrameSize));

                            // If amqp heartbeats are enabled, spawn a heartbeat agent...
                            if (state.ConnectionConfiguration.ConnectionIntegrityStrategy.HeartbeatFrequency.HasValue) {
                                var heartbeatAgent = context.SpawnNamed(
                                    name: "heartbeat",
                                    props: HeartbeatAgent.Create()
                                );
                                context.Send(state.FrameRouter, new AddRoutee(0, heartbeatAgent));
                                context.Send(heartbeatAgent, new StartHeartbeat(
                                    Dispatcher: state.Dispatcher,
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

                            _behaviour.Become(Connected(state.ToConnectedState(
                                availableChannelIds: Enumerable.Range(1, completed.MaxChannelCount)
                                    .Select(channelId => (UInt16)channelId)
                                    .ToImmutableList()
                            )));

                            state.Promise.SetResult();
                            break;
                        }
                        case HandshakeFailed failed: {
                            state.ScheduledTimeout.Cancel();
                            state.Promise.SetException(failed.Reason);
                            _behaviour.Become(Disconnected);
                            break;
                        }
                        case TimeoutExpired _: {
                            state.Promise.SetException(new TimeoutException("A connection to the broker was established but the negotiation did not complete within the specified connection timeout limit"));
                            context.Stop(state.Dispatcher);
                            context.Stop(state.FrameRouter);
                            context.Stop(state.HandshakeAgent);
                            context.Stop(state.SocketAgent);
                            _behaviour.Become(Disconnected);
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive Connected(ConnectedState state) =>
                async (IContext context) => {
                    switch (context.Message) {
                        case RemoteFlatline _: {
                            context.Stop(state.Dispatcher);
                            context.Stop(state.FrameRouter);
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
                            context.Send(state.FrameRouter, new AddRoutee(channelId, channelAgent));
                            var promise = new TaskCompletionSource();
                            context.Send(channelAgent, new Open(channelId, state.TxD!, promise));

                            try {
                                await promise.Task;
                                _behaviour.Become(Connected(state with {
                                    AvailableChannelIds = state.AvailableChannelIds.Remove(channelId)
                                }));
                                openChannel.Promise.SetResult(channelAgent);
                            }
                            catch (Exception fault) {
                                openChannel.Promise.SetException(fault);
                            }
                            break;
                        };
                    }
                };
        }
    }
}
