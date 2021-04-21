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

    using TimeoutExpired = Lapine.Agents.RabbitClientAgent.Protocol.TimeoutExpired;

    static public class RabbitClientAgent {
        static public class Protocol {
            public record Connect(ConnectionConfiguration Configuration, TaskCompletionSource Promise);
            public record OpenChannel(TaskCompletionSource<PID> Promise);
            internal record TimeoutExpired();
        }

        record ConnectingState(
            ConnectionConfiguration ConnectionConfiguration,
            TaskCompletionSource Promise,
            PID SocketAgent,
            IPEndPoint[] RemainingEndpoints,
            CancellationTokenSource CancelTimeout,
            IImmutableList<Exception> AccumulatedFailures
        );

        record NegotiatingState(
            ConnectionConfiguration ConnectionConfiguration,
            TaskCompletionSource Promise,
            PID SocketAgent,
            CancellationTokenSource CancelTimeout,
            PID TxD,
            PID FrameRouter,
            PID HandshakeAgent,
            PID Dispatcher
        ) {
            public NegotiatingState(ConnectingState state, PID txd, PID frameRouter, PID handshakeAgent, PID dispatcher)
                : this(state.ConnectionConfiguration, state.Promise, state.SocketAgent, state.CancelTimeout, txd, frameRouter, handshakeAgent, dispatcher) { }
        }

        record ConnectedState(
            ConnectionConfiguration ConnectionConfiguration,
            PID SocketAgent,
            PID TxD,
            PID FrameRouter,
            PID HeartbeatAgent,
            PID Dispatcher,
            IImmutableList<UInt16> AvailableChannelIds
        ) {
            public ConnectedState(NegotiatingState state, PID heartbeatAgent, IImmutableList<UInt16> availableChannelIds)
                : this(state.ConnectionConfiguration, state.SocketAgent, state.TxD, state.FrameRouter, heartbeatAgent, state.Dispatcher, availableChannelIds) { }
        }

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
                    case Connect connect: {
                        var state = new ConnectingState(
                            ConnectionConfiguration: connect.Configuration,
                            Promise: connect.Promise,
                            SocketAgent: context.SpawnNamed(
                                name: "socket",
                                props: SocketAgent.Create()
                            ),
                            RemainingEndpoints: connect.Configuration.GetConnectionSequence(),
                            CancelTimeout: context.Scheduler().SendOnce(connect.Configuration.ConnectionTimeout, context.Self!, new TimeoutExpired()),
                            AccumulatedFailures: ImmutableList<Exception>.Empty
                        );

                        if (state.RemainingEndpoints.Length > 0) {
                            context.Send(state.SocketAgent, new SocketAgent.Protocol.Connect(state.RemainingEndpoints[0], connect.Configuration.ConnectionTimeout, context.Self!));
                            _behaviour.Become(Connecting(state with { RemainingEndpoints = state.RemainingEndpoints[1..] }));
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
                        case SocketAgent.Protocol.ConnectionFailed failed: {
                            if (state.RemainingEndpoints.Length > 0) {
                                context.Send(state.SocketAgent, new SocketAgent.Protocol.Connect(state.RemainingEndpoints[0], state.ConnectionConfiguration!.ConnectionTimeout, context.Self!));
                                _behaviour.Become(Connecting(state with { RemainingEndpoints = state.RemainingEndpoints[1..], AccumulatedFailures = state.AccumulatedFailures.Add(failed.Reason) }));
                            }
                            else {
                                state.CancelTimeout.Cancel();
                                state.Promise.SetException(new AggregateException("Could not connect to any of the configured endpoints", state.AccumulatedFailures.Add(failed.Reason)));
                                context.Stop(state.SocketAgent);
                                _behaviour.Become(Disconnected);
                            }
                            break;
                        }
                        case SocketAgent.Protocol.Connected connected: {
                            var negotiatingState = new NegotiatingState(state, connected.TxD,
                                frameRouter: context.SpawnNamed(
                                    name: "frame_router",
                                    props: FrameRouterAgent.Create()
                                ),
                                handshakeAgent: context.SpawnNamed(
                                    name: "handshake",
                                    props: HandshakeAgent.Create()
                                ),
                                dispatcher: context.SpawnNamed(
                                    name: "dispatcher",
                                    props: DispatcherAgent.Create()
                                )
                            );

                            context.Send(connected.RxD, new SocketAgent.Protocol.BeginPolling(negotiatingState.FrameRouter));
                            context.Send(negotiatingState.FrameRouter, new AddRoutee(0, negotiatingState.HandshakeAgent));
                            context.Send(negotiatingState.Dispatcher, new DispatchTo(connected.TxD, 0));
                            context.Send(negotiatingState.HandshakeAgent, new BeginHandshake(state.ConnectionConfiguration!, context.Self!, negotiatingState.Dispatcher));
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
                            state.CancelTimeout.Cancel();
                            context.Stop(state.HandshakeAgent);
                            var availableChannels = Enumerable.Range(1, completed.MaxChannelCount)
                                .Select(channelId => (UInt16)channelId)
                                .ToImmutableList();

                            var connectedState = new ConnectedState(state,
                                heartbeatAgent: context.SpawnNamed(
                                    name: "heartbeat",
                                    props: HeartbeatAgent.Create()
                                ),
                                availableChannelIds: availableChannels
                            );

                            context.Send(connectedState.FrameRouter, new AddRoutee(0, connectedState.HeartbeatAgent));
                            context.Send(connectedState.HeartbeatAgent, new StartHeartbeat(state.Dispatcher, completed.HeartbeatFrequency, context.Self!));
                            context.Send(connectedState.SocketAgent, new SocketAgent.Protocol.Tune(completed.MaxFrameSize));

                            state.Promise.SetResult();

                            _behaviour.Become(Connected(connectedState));
                            break;
                        }
                        case HandshakeFailed failed: {
                            state.CancelTimeout.Cancel();
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
                (IContext context) => {
                    switch (context.Message) {
                        case RemoteFlatline _: {
                            context.Stop(state.Dispatcher);
                            context.Stop(state.FrameRouter);
                            context.Stop(state.HeartbeatAgent);
                            context.Stop(state.SocketAgent);

                            _behaviour.Become(Disconnected);
                            context.Send(context.Self!, new Connect(state.ConnectionConfiguration!, new TaskCompletionSource()));
                            break;
                        }
                        case OpenChannel openChannel: {
                            var channelId = state.AvailableChannelIds[0];
                            var channelAgent = context.SpawnNamed(
                                name: $"channel_{channelId}",
                                props: ChannelAgent.Create(state.ConnectionConfiguration.MaximumFrameSize)
                            );
                            context.Send(state.FrameRouter, new AddRoutee(channelId, channelAgent));
                            context.Send(channelAgent, new Open(context.Self!, channelId, state.TxD!));
                            _behaviour.Become(OpeningChannel(state, openChannel.Promise));
                            break;
                        };
                    }
                    return CompletedTask;
                };

            Receive OpeningChannel(ConnectedState state, TaskCompletionSource<PID> promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case Opened opened: {
                            promise.SetResult(opened.ChannelAgent);
                            _behaviour.Become(Connected(state with { AvailableChannelIds = state.AvailableChannelIds!.RemoveAt(0) }));
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }
    }
}
