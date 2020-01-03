namespace Lapine.Agents {
    using System;
    using System.Dynamic;
    using System.Threading.Tasks;
    using Lapine.Agents.Middleware;
    using Lapine.Protocol;
    using Proto;

    using static Lapine.Agents.Messages;
    using static Proto.Actor;

    public class AmqpClientAgent : IActor {
        readonly ConnectionConfiguration _connectionConfiguration;
        readonly Behavior _behaviour;
        readonly dynamic _state;

        public AmqpClientAgent(ConnectionConfiguration connectionConfiguration) {
            _connectionConfiguration = connectionConfiguration ?? throw new ArgumentNullException(nameof(connectionConfiguration));
            _behaviour               = new Behavior(Unstarted);
            _state                   = new ExpandoObject();
        }

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Unstarted(IContext context)  {
            switch (context.Message) {
                case Started _: {
                    SpawnSocketAgent(context);
                    _behaviour.Become(Disconnected);
                    break;
                }
            }
            return Done;
        }

        Task Disconnected(IContext context) {
            switch (context.Message) {
                case (Connect, PID listener): {
                    _state.ConnectionReadyListener = listener;
                    _state.EndpointEnumerator = _connectionConfiguration.GetEndpointEnumerator();
                    _behaviour.Become(Connecting);

                    TryNextEndpoint(context);
                    break;
                }
            }
            return Done;
        }

        Task Connecting(IContext context) {
            switch (context.Message) {
                // If the socket agent terminates whilst we're in the connecting state, it indicates that connecting was unsuccessful.
                // Restart the socket agent, and instruct it to try the next available endpoint...
                case Terminated message when message.Who == _state.SocketAgent: {
                    SpawnSocketAgent(context);
                    TryNextEndpoint(context, endpointsExhausted: () => {
                        context.Send(_state.ConnectionReadyListener, (ConnectionFailed));
                        _behaviour.Become(Disconnected);
                    });
                    break;
                }
                case (SocketConnected): {
                    SpawnChannelRouter(context);
                    SpawnPrincipalChannel(context);
                    _behaviour.Become(Connected);
                    break;
                }
            }
            return Done;
        }

        Task Connected(IContext context) {
            switch (context.Message) {
                case (Inbound, RawFrame frame): {
                    context.Forward(_state.ChannelRouter);
                    break;
                }
                case (Outbound, RawFrame frame): {
                    context.Forward(_state.SocketAgent);
                    break;
                }
                case (HandshakeCompleted): {
                    context.Send(_state.ConnectionReadyListener, (ConnectionReady));
                    break;
                }
                // If the socket agent terminates whilst we're in the connected state, it indicates that we've lost the connection.
                // Restart the socket agent, and try connecting again...
                case Terminated message when message.Who == _state.SocketAgent: {
                    SpawnSocketAgent(context);
                    _behaviour.Become(Disconnected);
                    context.Send(context.Self, (Connect, context.Self));
                    break;
                }
            }
            return Done;
        }

        void SpawnSocketAgent(IContext context) {
            _state.SocketAgent = context.SpawnNamed(
                name: "socket",
                props: Props.FromProducer(() => new SocketAgent())
                    .WithContextDecorator(LoggingContextDecorator.Create)
            );
        }

        void SpawnChannelRouter(IContext context) {
            _state.ChannelRouter = context.SpawnNamed(
                name: "channel-router",
                props: Props.FromProducer(() => new ChannelRouterAgent())
                    .WithContextDecorator(LoggingContextDecorator.Create)
            );
        }

        void SpawnPrincipalChannel(IContext context) {
            var principal = context.SpawnNamed(
                name: "channel-0",
                props: Props.FromProducer(() => new PrincipalChannelAgent(_connectionConfiguration))
                    .WithContextDecorator(LoggingContextDecorator.Create)
                    .WithReceiveMiddleware(FramingMiddleware.UnwrapInboundMethodFrames())
                    .WithSenderMiddleware(FramingMiddleware.WrapOutboundCommands(channel: 0))
            );
            context.Send(_state.ChannelRouter, (AddChannel, (UInt16)0, principal));
        }

        void TryNextEndpoint(IContext context, Action endpointsExhausted = null) {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            if (_state.EndpointEnumerator.MoveNext()) {
                var endpoint = _state.EndpointEnumerator.Current;
                context.Send(_state.SocketAgent, (Connect, endpoint));
            }
            else {
                endpointsExhausted?.Invoke();
            }
        }
    }
}
