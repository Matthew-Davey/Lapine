namespace Lapine.Agents.Middleware;

using Lapine.Protocol;
using Lapine.Protocol.Commands;
using Proto;

using static Lapine.Agents.SocketAgent.Protocol;

static class FramingMiddleware {
    static public Func<Receiver, Receiver> UnwrapInboundMethodFrames() =>
        next => (context, envelope) => {
            if (envelope.Message is FrameReceived { Frame: { Type: FrameType.Method } } received &&
                received.Frame.Payload.Span.ReadMethodHeader(out var methodHeader, out var buffer))
            {
                return methodHeader switch {
                    // Connection class
                    (0x0A, 0x0A) => ConnectionStart.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x0A, 0x14) => ConnectionSecure.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x0A, 0x1E) => ConnectionTune.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x0A, 0x29) => ConnectionOpenOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x0A, 0x32) => ConnectionClose.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x0A, 0x33) => ConnectionCloseOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    // Channel class
                    (0x14, 0x0B) => ChannelOpenOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x14, 0x14) => ChannelFlow.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x14, 0x15) => ChannelFlowOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x14, 0x28) => ChannelClose.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x14, 0x29) => ChannelCloseOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    // Exchange class
                    (0x28, 0x0B) => ExchangeDeclareOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x28, 0x15) => ExchangeDeleteOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    // Queue class
                    (0x32, 0x0B) => QueueDeclareOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x32, 0x15) => QueueBindOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x32, 0x33) => QueueUnbindOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x32, 0x1F) => QueuePurgeOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x32, 0x29) => QueueDeleteOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    // Basic class
                    (0x3C, 0x0B) => BasicQosOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x3C, 0x15) => BasicConsumeOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x3C, 0x1F) => BasicCancelOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x3C, 0x32) => BasicReturn.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x3C, 0x3C) => BasicDeliver.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x3C, 0x47) => BasicGetOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x3C, 0x48) => BasicGetEmpty.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x3C, 0x50) => BasicAck.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x3C, 0x6F) => BasicRecoverOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x3C, 0x78) => BasicNack.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    // TX class
                    (0x5A, 0x0B) => TransactionSelectOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x5A, 0x15) => TransactionCommitOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x5A, 0x1F) => TransactionRollback.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    // Confirm class
                    (0x55, 0x0A) => ConfirmSelect.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    (0x55, 0x0B) => ConfirmSelectOk.Deserialize(in buffer, out var command, out _)
                                        ? next(context, envelope.WithMessage(command))
                                        : next(context, envelope),
                    _ => throw new Exception() // Unknown method...
                };
            }
            else {
                return next(context, envelope);
            }
        };

    static public Func<Receiver, Receiver> UnwrapInboundContentHeaderFrames() =>
        next => (context, envelope) => {
            if (envelope.Message is FrameReceived { Frame: { Type: FrameType.Header } } received) {
                return ContentHeader.Deserialize(received.Frame.Payload.Span, out var contentHeader, out var _)
                    ? next(context, envelope.WithMessage(contentHeader))
                    : next(context, envelope);
            }
            else {
                return next(context, envelope);
            }
        };

    static public Func<Receiver, Receiver> UnwrapInboundContentBodyFrames() =>
        next => (context, envelope) => {
            if (envelope.Message is FrameReceived { Frame: { Type: FrameType.Body } } received) {
                return next(context, envelope.WithMessage(received.Frame.Payload));
            }
            else {
                return next(context, envelope);
            }
        };
}
