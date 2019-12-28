namespace Lapine.Agents {
    using System;
    using System.Threading.Tasks;
    using Lapine.Agents.Events;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;

    using static Proto.Actor;

    public class FrameHandlerAgent : IActor {
        readonly PID _listener;
        readonly Behavior _behaviour;

        public FrameHandlerAgent(PID listener) {
            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            _behaviour = new Behavior(Unstarted);
        }

        public Task ReceiveAsync(IContext context) =>
            _behaviour.ReceiveAsync(context);

        Task Unstarted(IContext context) {
            switch (context.Message) {
                case Started _: {
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
                        FrameType.Method    => HandleMethodFrame(in context, message.Frame),
                        FrameType.Header    => HandleHeaderFrame(in context, message.Frame),
                        FrameType.Body      => HandleBodyFrame(in context, message.Frame),
                        FrameType.Heartbeat => HandleHeartbeatFrame(in context, message.Frame),
                        _                   => Done
                    };
                }
            }
            return Done;
        }

        Task HandleMethodFrame(in IContext context, in RawFrame frame) {
            if (frame.Payload.Span.ReadMethodHeader(out var methodHeader, out var surplus)) {
                switch (methodHeader) {
                    case (0x0A, 0x0A): { // ConnectionStart
                        if (ConnectionStart.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                    case (0x0A, 0x14): { // ConnectionSecure
                        if (ConnectionSecure.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                    case (0x0A, 0x1E): { // ConnectionTune
                        if (ConnectionTune.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                    case (0x0A, 0x29): { // ConnectionOpenOk
                        if (ConnectionOpenOk.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                    case (0x0A, 0x32): { // ConnectionClose
                        if (ConnectionClose.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                    case (0x0A, 0x33): { // ConnectionCloseOk
                        if (ConnectionCloseOk.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                    case (0x14, 0x0B): { // ChannelOpenOk
                        if (ChannelOpenOk.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                    case (0x14, 0x14): { // ChannelFlow
                        if (ChannelFlow.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                    case (0x14, 0x15): { // ChannelFlowOk
                        if (ChannelFlowOk.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                    case (0x14, 0x28): { // ChannelClose
                        if (ChannelClose.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                    case (0x14, 0x29): { // ChannelCloseOk
                        if (ChannelCloseOk.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                    case (0x28, 0x0B): { // ExchangeDeclareOk
                        if (ExchangeDeclareOk.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                    case (0x28, 0x15): { // ExchangeDeletedOk
                        if (ExchangeDeleteOk.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                    case (0x32, 0x0B): { // QueueDeclareOk
                        if (QueueDeclareOk.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                    case (0x32, 0x15): { // QueueBindOk
                        if (QueueBindOk.Deserialize(in surplus, out var command, out surplus)) {
                            context.Send(_listener, command);
                        }
                        break;
                    }
                }
            }
            return Done;
        }

        Task HandleHeaderFrame(in IContext context, in RawFrame frame) {
            // TODO: Handle header frame...
            return Done;
        }

        Task HandleBodyFrame(in IContext context, in RawFrame frame) {
            // TODO: Handle body frame...
            return Done;
        }

        Task HandleHeartbeatFrame(in IContext context, in RawFrame frame) {
            // TODO: Handle heartbeat frame...
            return Done;
        }
    }
}
