namespace Lapine.Agents.Middleware {
    using System;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;

    static class FramingMiddleware {
        static public Func<Receiver, Receiver> UnwrapInboundMethodFrames() =>
            next => (context, envelope) => {
                if (envelope.Message is (":receive", RawFrame frame) &&
                    frame.Type is FrameType.Method &&
                    frame.Payload.Span.ReadMethodHeader(out var methodHeader, out var buffer))
                {
                    return methodHeader switch {
                        (0x0A, 0x0A) => ConnectionStart.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x0A, 0x14) => ConnectionSecure.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x0A, 0x1E) => ConnectionTune.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x0A, 0x29) => ConnectionOpenOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x0A, 0x32) => ConnectionClose.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x0A, 0x33) => ConnectionCloseOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x14, 0x0B) => ChannelOpenOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x14, 0x14) => ChannelFlow.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x14, 0x15) => ChannelFlowOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x14, 0x28) => ChannelClose.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x14, 0x29) => ChannelCloseOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x28, 0x0B) => ExchangeDeclareOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x28, 0x15) => ExchangeDeleteOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x32, 0x0B) => QueueDeclareOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x32, 0x15) => QueueBindOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x32, 0x33) => QueueUnbindOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x32, 0x1F) => QueuePurgeOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x32, 0x29) => QueueDeleteOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x3C, 0x0B) => BasicQosOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x3C, 0x15) => BasicConsumeOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x3C, 0x1F) => BasicCancelOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x3C, 0x32) => BasicReturn.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x3C, 0x3C) => BasicDeliver.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x3C, 0x47) => BasicGetOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x3C, 0x48) => BasicGetEmpty.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x3C, 0x6F) => BasicRecoverOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x5A, 0x0B) => TransactionSelectOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x5A, 0x15) => TransactionCommitOk.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        (0x5A, 0x1F) => TransactionRollback.Deserialize(in buffer, out var command, out _)
                                            ? next(context, envelope.WithMessage((":receive", command)))
                                            : next(context, envelope),
                        _ => throw new Exception() // Unknown method...
                    };
                }
                else {
                    return next(context, envelope);
                }
            };

        static public Func<Sender, Sender> WrapOutboundCommands(UInt16 channel) =>
            next => (context, pid, envelope) => {
                if (envelope.Message is (":transmit", ICommand command)) {
                    var frame = RawFrame.Wrap(in channel, in command);
                    return next(context, pid, envelope.WithMessage((":transmit", frame)));
                }
                else {
                    return next(context, pid, envelope);
                }
            };
    }
}
