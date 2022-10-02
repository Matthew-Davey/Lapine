namespace Lapine.Protocol;

using System.Buffers;
using Lapine.Protocol.Commands;

record MethodFrame(UInt16 Channel, (UInt16, UInt16) MethodHeader, ICommand Command) : Frame(FrameType.Method, Channel) {
    static public Boolean Deserialize(UInt16 channel, in ReadOnlySpan<Byte> buffer, out Frame? result) {
        if (buffer.ReadMethodHeader(out var header, out var surplus)) {
            switch (header) {
                // Connection class
                case (0x0A, 0x0A): {
                    if (ConnectionStart.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x0A, 0x14): {
                    if (ConnectionSecure.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x0A, 0x1E): {
                    if (ConnectionTune.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x0A, 0x29): {
                    if (ConnectionOpenOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x0A, 0x32): {
                    if (ConnectionClose.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x0A, 0x33): {
                    if (ConnectionCloseOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                // Channel class
                case (0x14, 0x0B): {
                    if (ChannelOpenOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x14, 0x14): {
                    if (ChannelFlow.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x14, 0x15): {
                    if (ChannelFlowOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x14, 0x28): {
                    if (ChannelClose.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x14, 0x29): {
                    if (ChannelCloseOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                // Exchange class
                case (0x28, 0x0B): {
                    if (ExchangeDeclareOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x28, 0x15): {
                    if (ExchangeDeleteOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                // Queue class
                case (0x32, 0x0B): {
                    if (QueueDeclareOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x32, 0x15): {
                    if (QueueBindOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x32, 0x33): {
                    if (QueueUnbindOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x32, 0x1F): {
                    if (QueuePurgeOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x32, 0x29): {
                    if (QueueDeleteOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                // Basic class
                case (0x3C, 0x0B): {
                    if (BasicQosOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x15): {
                    if (BasicConsumeOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x1F): {
                    if (BasicCancelOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x32): {
                    if (BasicReturn.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x3C): {
                    if (BasicDeliver.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x47): {
                    if (BasicGetOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x48): {
                    if (BasicGetEmpty.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x50): {
                    if (BasicAck.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x6F): {
                    if (BasicRecoverOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x78): {
                    if (BasicNack.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                // TX class
                case (0x5A, 0x0B): {
                    if (TransactionSelectOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x5A, 0x15): {
                    if (TransactionCommitOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x5A, 0x1F): {
                    if (TransactionRollback.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                // Confirm class
                case (0x55, 0x0A): {
                    if (ConfirmSelect.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x55, 0x0B): {
                    if (ConfirmSelectOk.Deserialize(in surplus, out var command, out surplus)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                default: {
                    throw new Exception("Unknown method header");
                }
            }
        }

        result = default;
        return false;
    }

    public override IBufferWriter<Byte> Serialize(IBufferWriter<Byte> buffer) {
        var payloadWriter = new ArrayBufferWriter<Byte>();
        payloadWriter.WriteMethodHeader(MethodHeader)
            .WriteSerializable(Command);

        return buffer.WriteUInt8((Byte)Type)
            .WriteUInt16BE(Channel)
            .WriteUInt32BE((UInt32)payloadWriter.WrittenCount)
            .WriteBytes(payloadWriter.WrittenSpan)
            .WriteUInt8(FrameTerminator);
    }
}
