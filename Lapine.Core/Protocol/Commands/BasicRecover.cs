namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct BasicRecover(Boolean ReQueue) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x6E);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteBoolean(ReQueue);

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicRecover? result) {
        if (buffer.ReadBoolean(out var requeue)) {
            result = new BasicRecover(requeue);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

record struct BasicRecoverOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x6F);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicRecoverOk? result) {
        result = new BasicRecoverOk();
        return true;
    }
}
