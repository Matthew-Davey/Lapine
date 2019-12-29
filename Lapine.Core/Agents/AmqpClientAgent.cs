namespace Lapine.Agents {
    using System;
    using System.Dynamic;
    using System.Threading.Tasks;
    using Lapine.Agents.Commands;
    using Lapine.Agents.Events;
    using Lapine.Agents.Middleware;
    using Proto;

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
                case Connect _: {
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
                        throw new Exception("None of the specified endpoints were reachable");
                    });
                    break;
                }
                case SocketConnected _: {
                    _behaviour.Become(Connected);
                    break;
                }
            }
            return Done;
        }

        Task Connected(IContext context) {
            switch (context.Message) {
                // If the socket agent terminates whilst we're in the connected state, it indicates that we've lost the connection.
                // Restart the socket agent, and try connecting again...
                case Terminated message when message.Who == _state.SocketAgent: {
                    SpawnSocketAgent(context);
                    _behaviour.Become(Disconnected);
                    context.Send(context.Self, new Connect());
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

        void TryNextEndpoint(IContext context, Action endpointsExhausted = null) {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            if (_state.EndpointEnumerator.MoveNext()) {
                var endpoint = _state.EndpointEnumerator.Current;
                context.Send(_state.SocketAgent, new SocketConnect(endpoint));
            }
            else {
                endpointsExhausted?.Invoke();
            }
        }
    }
}
