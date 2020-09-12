namespace Lapine.Agents {
    using System;
    using System.Dynamic;
    using System.Threading.Tasks;
    using Lapine.Protocol.Commands;
    using Proto;

    using static Proto.Actor;

    class ChannelAgent : IActor {
        readonly PID _listener;
        readonly Behavior _behaviour;
        readonly dynamic _state;

        public ChannelAgent(PID listener) {
            _listener  = listener;
            _behaviour = new Behavior(Unstarted);
            _state     = new ExpandoObject();
        }

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Unstarted(IContext context) {
            switch (context.Message) {
                case Started _: {
                    _behaviour.Become(Closed);
                    break;
                }
            }
            return Done;
        }

        Task Closed(IContext context) {
            switch (context.Message) {
                case (":open", PID listener): {
                    _state.ChannelOpenListener = listener;
                    context.Send(_listener, (":transmit", new ChannelOpen()));
                    _behaviour.Become(AwaitingChannelOpenOk);
                    break;
                }
            }
            return Done;
        }

        Task AwaitingChannelOpenOk(IContext context) {
            switch (context.Message) {
                case (":receive", ChannelOpenOk message): {
                    context.Send(_state.ChannelOpenListener, (":channel-opened", context.Self));
                    _behaviour.Become(Open);
                    break;
                }
            }
            return Done;
        }

        Task Open(IContext context) {
            switch (context.Message) {
                case (":receive", ChannelClose message): {
                    context.Send(_listener, (":transmit", new ChannelCloseOk()));
                    _behaviour.Become(Closed);
                    break;
                }
            }
            return Done;
        }
    }
}
