namespace Lapine.Agents {
    using System.Threading.Tasks;
    using Lapine.Agents.Events;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;

    using static Proto.Actor;

    public class FrameHandlerAgent : IActor {
        readonly Behavior _behaviour;

        public FrameHandlerAgent() =>
            _behaviour = new Behavior(Unstarted);

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Unstarted(IContext context) {
            switch (context.Message) {
                case Started _: {
                    Actor.EventStream.Subscribe<FrameReceived>(message => context.Send(context.Self, message));
                    _behaviour.Become(Started);
                    return Done;
                }
            }
            return Done;
        }

        Task Started(IContext context) {
            switch (context.Message) {
                case FrameReceived message: {
                    return message.Frame.Type switch {
                        FrameType.Method    => HandleMethodFrame(message.Frame),
                        FrameType.Header    => HandleHeaderFrame(message.Frame),
                        FrameType.Body      => HandleBodyFrame(message.Frame),
                        FrameType.Heartbeat => HandleHeartbeatFrame(message.Frame),
                        _                   => Done
                    };
                }
            }
            return Done;
        }

        Task HandleMethodFrame(in RawFrame frame) {
            if (frame.Payload.Span.ReadMethodHeader(out var methodHeader, out var surplus)) {
                switch (methodHeader) {
                    case (0x0A, 0x0A): { // ConnectionStart
                        if (ConnectionStart.Deserialize(in surplus, out var command, out surplus)) {
                            Actor.EventStream.Publish(command);
                        }
                        break;
                    }
                }
            }
            return Done;
        }

        Task HandleHeaderFrame(in RawFrame frame) {
            // TODO: Handle header frame...
            return Done;
        }

        Task HandleBodyFrame(in RawFrame frame) {
            // TODO: Handle body frame...
            return Done;
        }

        Task HandleHeartbeatFrame(in RawFrame frame) {
            // TODO: Handle heartbeat frame...
            return Done;
        }
    }
}
