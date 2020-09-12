namespace Lapine.Agents {
    using System;
    using System.Dynamic;
    using System.Threading.Tasks;
    using Lapine.Protocol.Commands;
    using Proto;

    using static Proto.Actor;

    class ChannelAgent : IActor {
        readonly PID _listener;
        readonly UInt16 _channelNumber;
        readonly Behavior _behaviour;
        readonly dynamic _state;

        public ChannelAgent(PID listener, UInt16 channelNumber) {
            _listener      = listener;
            _channelNumber = channelNumber;
            _behaviour     = new Behavior(Unstarted);
            _state         = new ExpandoObject();
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
                case (":close", PID listener): {
                    _state.ChannelCloseListener = listener;
                    context.Send(_listener, (":transmit", new ChannelClose(0, "Channel closed by client", (0, 0))));
                    _behaviour.Become(AwaitingChannelCloseOk);
                    break;
                }
            }
            return Done;
        }

        Task AwaitingChannelCloseOk(IContext context) {
            switch (context.Message) {
                case (":receive", ChannelCloseOk _): {
                    context.Send(_state.ChannelCloseListener, (":channel-closed", _channelNumber));
                    context.Send(_listener, (":channel-closed", _channelNumber));
                    _behaviour.Become(Closed);
                    break;
                }
            }
            return Done;
        }
    }
}
