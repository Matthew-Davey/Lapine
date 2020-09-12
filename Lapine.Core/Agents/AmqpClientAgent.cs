namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Threading.Tasks;
    using Lapine.Agents.Middleware;
    using Lapine.Protocol;
    using Proto;

    using static Proto.Actor;

    class AmqpClientAgent : IActor {
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
                case (":connect", PID listener): {
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
                        context.Send(_state.ConnectionReadyListener, (":connection-failed"));
                        _behaviour.Become(Disconnected);
                    });
                    break;
                }
                case (":socket-connected"): {
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
                case (":receive", RawFrame frame): {
                    context.Forward(_state.ChannelRouter);
                    break;
                }
                case (":transmit", RawFrame frame): {
                    context.Forward(_state.SocketAgent);
                    break;
                }
                case (":handshake-completed", UInt16 maximumChannelCount): {
                    _state.AvailableChannels = new Queue<UInt16>(Enumerable.Range(1, maximumChannelCount).Select(x => (UInt16)x));
                    context.Send(_state.ConnectionReadyListener, (":connection-ready"));
                    break;
                }
                case (":open-channel", PID listener): {
                    var channelNumber = _state.AvailableChannels.Dequeue();
                    var channel = SpawnChannel(context, channelNumber);
                    context.Send(channel, (":open", listener));
                    break;
                }
                case (":channel-closed", PID channel): {
                    context.Forward(_state.ChannelRouter);
                    break;
                }
                // If the socket agent terminates whilst we're in the connected state, it indicates that we've lost the connection.
                // Restart the socket agent, and try connecting again...
                case Terminated message when message.Who == _state.SocketAgent: {
                    SpawnSocketAgent(context);
                    _behaviour.Become(Disconnected);
                    context.Send(context.Self, (":connect", context.Self));
                    break;
                }
            }
            return Done;
        }

        void SpawnSocketAgent(IContext context) =>
            _state.SocketAgent = context.SpawnNamed(
                name: "socket",
                props: Props.FromProducer(() => new SocketAgent(context.Self))
                    .WithContextDecorator(LoggingContextDecorator.Create)
            );

        void SpawnChannelRouter(IContext context) =>
            _state.ChannelRouter = context.SpawnNamed(
                name: "channel-router",
                props: Props.FromProducer(() => new ChannelRouterAgent())
                    .WithContextDecorator(LoggingContextDecorator.Create)
            );

        void SpawnPrincipalChannel(IContext context) {
            var principal = context.SpawnNamed(
                name: "channel-0",
                props: Props.FromProducer(() => new PrincipalChannelAgent(_connectionConfiguration))
                    .WithContextDecorator(LoggingContextDecorator.Create)
                    .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames())
                    .WithSenderMiddleware(FramingMiddleware.WrapOutboundCommands(channel: 0))
            );
            context.Send(_state.ChannelRouter, (":add-channel", (UInt16)0, principal));
        }

        PID SpawnChannel(IContext context, UInt16 channelNumber) {
            var channel = context.SpawnNamed(
                name: $"channel-{channelNumber}",
                props: Props.FromProducer(() => new ChannelAgent(context.Self, channelNumber))
                    .WithContextDecorator(LoggingContextDecorator.Create)
                    .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames())
                    .WithSenderMiddleware(FramingMiddleware.WrapOutboundCommands(channel: channelNumber))
            );
            context.Send(_state.ChannelRouter, (":add-channel", (UInt16)channelNumber, channel));
            return channel;
        }

        void TryNextEndpoint(IContext context, Action endpointsExhausted = null) {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            if (_state.EndpointEnumerator.MoveNext()) {
                var endpoint = _state.EndpointEnumerator.Current;
                context.Send(_state.SocketAgent, (":connect", endpoint));
            }
            else {
                endpointsExhausted?.Invoke();
            }
        }
    }
}
