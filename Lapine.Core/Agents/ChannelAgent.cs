namespace Lapine.Agents {
    using System;
    using System.Dynamic;
    using System.Threading.Tasks;
    using Lapine.Protocol.Commands;
    using Proto;

    using static Proto.Actor;

    public class ChannelAgent : IActor {
        readonly Behavior _behaviour;
        readonly dynamic _state;

        public ChannelAgent() {
            _behaviour = new Behavior(Unstarted);
            _state     = new ExpandoObject();
        }

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Unstarted(IContext context) {
            switch (context.Message) {
                case Started _: {
                    _behaviour.Become(Running);
                    break;
                }
            }
            return Done;
        }

        Task Running(IContext context) {
            switch (context.Message) {
                case (":inbound", ConnectionClose message): {
                    context.Send(context.Parent, (":transmit", new ConnectionCloseOk()));
                    context.Self.Stop();
                    return Done;
                }
            }
            return Done;
        }
    }
}
