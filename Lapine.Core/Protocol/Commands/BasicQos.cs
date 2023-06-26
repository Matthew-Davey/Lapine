namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

record struct BasicQos(UInt32 PrefetchSize, UInt16 PrefetchCount, Boolean Global) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x0A);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt32BE(PrefetchSize)
            .WriteUInt16BE(PrefetchCount)
            .WriteBoolean(Global);

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicQos? result) {
        if (buffer.ReadUInt32BE(out var prefetchSize) &&
            buffer.ReadUInt16BE(out var prefetchCount) &&
            buffer.ReadBoolean(out var global))
        {
            result = new BasicQos(prefetchSize, prefetchCount, global);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

record struct BasicQosOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x0B);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicQosOk? result) {
        result = new BasicQosOk();
        return true;
    }
}
