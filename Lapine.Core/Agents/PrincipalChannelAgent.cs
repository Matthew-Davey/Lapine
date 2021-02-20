namespace Lapine.Agents {
    using System;
    using System.Dynamic;
    using System.Threading.Tasks;
    using Lapine.Agents.Middleware;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;

    using static System.Threading.Tasks.Task;

    class PrincipalChannelAgent : IActor {
        readonly Behavior _behaviour;
        readonly ConnectionConfiguration _connectionConfiguration;
        readonly dynamic _state;

        public PrincipalChannelAgent(ConnectionConfiguration connectionConfiguration) {
            _behaviour               = new Behavior(Unstarted);
            _connectionConfiguration = connectionConfiguration ?? throw new ArgumentNullException(nameof(connectionConfiguration));
            _state                   = new ExpandoObject();
        }

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Unstarted(IContext context) {
            switch (context.Message) {
                case Started _: {
                    _behaviour.Become(AwaitConnectionStart);
                    break;
                }
            }
            return CompletedTask;
        }

        Task AwaitConnectionStart(IContext context) {
            switch (context.Message) {
                case (":receive", ConnectionStart message): {
                    _state.HandshakeAgent = context.SpawnNamed(
                        name: "handshake",
                        props: Props.FromProducer(() => new HandshakeAgent(context.Self!, _connectionConfiguration))
                            .WithContextDecorator(LoggingContextDecorator.Create)
                    );
                    context.Forward(_state.HandshakeAgent);
                    _behaviour.Become(Negotiating);
                    break;
                }
            }
            return CompletedTask;
        }

        Task Negotiating(IContext context) {
            switch (context.Message) {
                case (":transmit", _): {
                    if (context.Parent != null)
                        context.Forward(context.Parent);
                    return CompletedTask;
                }
                case (":receive", ICommand _): {
                    context.Forward(_state.HandshakeAgent);
                    return CompletedTask;
                }
                case (":start-heartbeat-transmission", UInt16 frequency): {
                    _state.HeartbeatAgent = context.SpawnNamed(
                        name: "heartbeat",
                        props: Props.FromProducer(() => new HeartbeatAgent(context.Self!))
                            .WithContextDecorator(LoggingContextDecorator.Create)
                    );
                    context.Forward(_state.HeartbeatAgent);
                    return CompletedTask;
                }
                case (":handshake-completed", UInt16 MaximumChannelCount): {
                    _behaviour.UnbecomeStacked();
                    if (context.Parent != null)
                        context.Forward(context.Parent);
                    _behaviour.Become(Open);
                    return CompletedTask;
                }
                case (":authentication-failed"): {
                    context.Stop(context.Self!); // TODO: fail gracefully
                    return CompletedTask;
                }
            }
            return CompletedTask;
        }

        Task Open(IContext context) {
            switch (context.Message) {
                case (":receive", RawFrame frame): {
                    context.Forward(_state.HeartbeatAgent);
                    break;
                }
                case (":transmit", _): {
                    if (context.Parent != null)
                        context.Forward(context.Parent);
                    break;
                }
                case (":remote-flatline", DateTime lastRemoteHeartbeat): {
                    context.Stop(context.Self!); // TODO: How to best respond to a remote flatline?
                    _behaviour.Become(Closed);
                    break;
                }
                case (":receive", ConnectionClose message): {
                    if (context.Parent != null)
                        context.Send(context.Parent, (":transmit", new ConnectionCloseOk()));
                    context.Stop(context.Self!);
                    _behaviour.Become(Closed);
                    break;
                }
            }
            return CompletedTask;
        }

        Task Closed(IContext context) => CompletedTask;
    }
}
