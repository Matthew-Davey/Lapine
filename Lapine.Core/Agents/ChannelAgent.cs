namespace Lapine.Agents {
    using System;
    using System.Threading.Tasks;
    using Lapine.Agents.Commands;
    using Proto;

    public class ChannelAgent : IActor {
        readonly Behavior _behaviour;

        public ChannelAgent() =>
            _behaviour = new Behavior(Unstarted);

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Unstarted(IContext context) {
            switch (context.Message) {
                case Started _: {
                    _behaviour.Become(Running);
                    break;
                }
            }
            return Actor.Done;
        }

        Task Running(IContext context) {
            switch (context.Message) {
                case TransmitCommand message: {
                    context.Send(context.Parent, message.Command);
                    return Actor.Done;
                }
            }
            return Actor.Done;
        }
    }
}
