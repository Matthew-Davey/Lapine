namespace Lapine.Agents {
    using System;
    using System.Threading.Tasks;
    using Lapine.Agents.Commands;
    using Lapine.Agents.Events;
    using Lapine.Protocol;
    using Proto;
    using Proto.Router;

    public class ChannelAgent : IActor {
        readonly UInt16 _channelNumber;
        readonly Behavior _behaviour;
        PID _broadcastGroup;
        PID _frameHandler;

        public ChannelAgent(UInt16 channelNumber) {
            _channelNumber = channelNumber;
            _behaviour     = new Behavior(Unstarted);
        }

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Unstarted(IContext context) {
            switch (context.Message) {
                case Started _: {
                    _broadcastGroup = context.Spawn(
                        Router.NewBroadcastGroup()
                              .WithChildSupervisorStrategy(new AlwaysRestartStrategy())
                    );

                    _frameHandler = context.Spawn(
                        Props.FromProducer(() => new FrameHandlerAgent(_broadcastGroup))
                             .WithChildSupervisorStrategy(new AlwaysRestartStrategy())
                    );

                    _behaviour.Become(Running);

                    break;
                }
            }
            return Actor.Done;
        }

        Task Running(IContext context) {
            switch (context.Message) {
                case FrameReceived message: {
                    context.Forward(_frameHandler);
                    return Actor.Done;
                }
                case TransmitCommand message: {
                    var frame = RawFrame.Wrap(_channelNumber, message.Command);
                    context.Send(context.Parent, new SocketTransmit(frame));
                    return Actor.Done;
                }
            }
            return Actor.Done;
        }
    }
}
