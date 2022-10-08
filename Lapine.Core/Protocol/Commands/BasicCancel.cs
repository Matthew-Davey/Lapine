namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct BasicCancel(String ConsumerTag, Boolean NoWait): ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x1E);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteShortString(ConsumerTag)
            .WriteBoolean(NoWait);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out BasicCancel? result) {
        if (BufferExtensions.ReadShortString(ref buffer, out var consumerTag) &&
            BufferExtensions.ReadBoolean(ref buffer, out var noWait))
        {
            result = new BasicCancel(consumerTag, noWait);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

readonly record struct BasicCancelOk(String ConsumerTag) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x1F);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteShortString(ConsumerTag);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out BasicCancelOk? result) {
        if (BufferExtensions.ReadShortString(ref buffer, out var consumerTag)) {
            result = new BasicCancelOk(consumerTag);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
