namespace Lapine.Agents.Middleware {
    using System;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;

    public static class FramingMiddleware {
        static public Func<Receiver, Receiver> UnwrapFrames(UInt16 channel) =>
            (next => (context, envelope) => {
                switch (envelope.Message) {
                    case RawFrame frame when frame.Type == FrameType.Method: {
                        if (frame.Payload.Span.ReadMethodHeader(out var methodHeader, out var surplus)) {
                            switch (methodHeader) {
                                case (0x0A, 0x0A): { // ConnectionStart
                                    if (ConnectionStart.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x0A, 0x14): { // ConnectionSecure
                                    if (ConnectionSecure.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x0A, 0x1E): { // ConnectionTune
                                    if (ConnectionTune.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x0A, 0x29): { // ConnectionOpenOk
                                    if (ConnectionOpenOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x0A, 0x32): { // ConnectionClose
                                    if (ConnectionClose.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x0A, 0x33): { // ConnectionCloseOk
                                    if (ConnectionCloseOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x14, 0x0B): { // ChannelOpenOk
                                    if (ChannelOpenOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x14, 0x14): { // ChannelFlow
                                    if (ChannelFlow.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x14, 0x15): { // ChannelFlowOk
                                    if (ChannelFlowOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x14, 0x28): { // ChannelClose
                                    if (ChannelClose.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x14, 0x29): { // ChannelCloseOk
                                    if (ChannelCloseOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x28, 0x0B): { // ExchangeDeclareOk
                                    if (ExchangeDeclareOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x28, 0x15): { // ExchangeDeletedOk
                                    if (ExchangeDeleteOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x32, 0x0B): { // QueueDeclareOk
                                    if (QueueDeclareOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x32, 0x15): { // QueueBindOk
                                    if (QueueBindOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x32, 0x33): { // QueueUnbindOk
                                    if (QueueUnbindOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x32, 0x1F): { // QueuePurgeOk
                                    if (QueuePurgeOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x32, 0x29): { // QueueDeleteOk
                                    if (QueueDeleteOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x3C, 0x0B): { // BasicQos
                                    if (BasicQosOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x3C, 0x15): { // BasicConsumeOk
                                    if (BasicConsumeOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x3C, 0x1F): { // BasicCancelOk
                                    if (BasicCancelOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x3C, 0x32): { // BasicReturn
                                    if (BasicReturn.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x3C, 0x3C): { // BasicDeliver
                                    if (BasicDeliver.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x3C, 0x47): { // BasicGetOk
                                    if (BasicGetOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x3C, 0x48): { // BasicGetEmpty
                                    if (BasicGetEmpty.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x3C, 0x6F): { // BasicRecoverOk
                                    if (BasicRecoverOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x5A, 0x0B): { // TransactionSelectOk
                                    if (TransactionSelectOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x5A, 0x15): { // TransactionCommitOk
                                    if (TransactionCommitOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                case (0x5A, 0x1F): { // TransactionRollback
                                    if (TransactionRollbackOk.Deserialize(in surplus, out var command, out surplus)) {
                                        return next(context, envelope.WithMessage(command));
                                    }
                                    break;
                                }
                                default: {
                                    throw new Exception(); // Unknown method...
                                }
                            }
                        }

                        throw new FramingErrorException();
                    }
                    // case RawFrame frame when frame.Type == FrameType.Header:
                    // case RawFrame frame when frame.Type == FrameType.Body:
                    // case RawFrame frame when frame.Type == FrameType.Heartbeat:
                    default: return next(context, envelope);
                }
            });

        static public Func<Sender, Sender> WrapCommands(UInt16 channel) =>
            (next => (context, target, envelope) => {
                switch (envelope.Message) {
                    case ICommand command: {
                        var frame = RawFrame.Wrap(in channel, in command);
                        return next(context, target, envelope.WithMessage(frame));
                    }
                    default: return next(context, target, envelope);
                }
            });
    }
}
