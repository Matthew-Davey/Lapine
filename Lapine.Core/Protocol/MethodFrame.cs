namespace Lapine.Protocol;

using System.Buffers;
using Lapine.Protocol.Commands;

record MethodFrame(UInt16 Channel, (UInt16, UInt16) MethodHeader, ICommand Command) : Frame(FrameType.Method, Channel) {
    static public Boolean Deserialize(UInt16 channel, ref ReadOnlyMemory<Byte> buffer, out Frame? result) {
        if (BufferExtensions.ReadMethodHeader(ref buffer, out var header)) {
            switch (header) {
                // Connection class
                case (0x0A, 0x0A): {
                    if (ConnectionStart.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x0A, 0x14): {
                    if (ConnectionSecure.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x0A, 0x1E): {
                    if (ConnectionTune.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x0A, 0x29): {
                    if (ConnectionOpenOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x0A, 0x32): {
                    if (ConnectionClose.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x0A, 0x33): {
                    if (ConnectionCloseOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                // Channel class
                case (0x14, 0x0B): {
                    if (ChannelOpenOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x14, 0x14): {
                    if (ChannelFlow.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x14, 0x15): {
                    if (ChannelFlowOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x14, 0x28): {
                    if (ChannelClose.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x14, 0x29): {
                    if (ChannelCloseOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                // Exchange class
                case (0x28, 0x0B): {
                    if (ExchangeDeclareOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x28, 0x15): {
                    if (ExchangeDeleteOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                // Queue class
                case (0x32, 0x0B): {
                    if (QueueDeclareOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x32, 0x15): {
                    if (QueueBindOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x32, 0x33): {
                    if (QueueUnbindOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x32, 0x1F): {
                    if (QueuePurgeOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x32, 0x29): {
                    if (QueueDeleteOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                // Basic class
                case (0x3C, 0x0B): {
                    if (BasicQosOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x15): {
                    if (BasicConsumeOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x1F): {
                    if (BasicCancelOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x32): {
                    if (BasicReturn.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x3C): {
                    if (BasicDeliver.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x47): {
                    if (BasicGetOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x48): {
                    if (BasicGetEmpty.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x50): {
                    if (BasicAck.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x6F): {
                    if (BasicRecoverOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x3C, 0x78): {
                    if (BasicNack.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                // TX class
                case (0x5A, 0x0B): {
                    if (TransactionSelectOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x5A, 0x15): {
                    if (TransactionCommitOk.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x5A, 0x1F): {
                    if (TransactionRollback.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                // Confirm class
                case (0x55, 0x0A): {
                    if (ConfirmSelect.Deserialize(ref buffer, out var command)) {
                        result = new MethodFrame(channel, header, command);
                        return true;
                    }
                    break;
                }
                case (0x55, 0x0B): {
                    if (ConfirmSelectOk.Deserialize(ref buffer, out var command)) {
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
        var payloadWriter = new MemoryBufferWriter<Byte>();
        payloadWriter.WriteMethodHeader(MethodHeader)
            .WriteSerializable(Command);

        return buffer.WriteUInt8((Byte)Type)
            .WriteUInt16BE(Channel)
            .WriteUInt32BE((UInt32)payloadWriter.WrittenCount)
            .WriteBytes(payloadWriter.WrittenMemory)
            .WriteUInt8(FrameTerminator);
    }
}
