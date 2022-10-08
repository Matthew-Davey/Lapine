namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct BasicQos(UInt32 PrefetchSize, UInt16 PrefetchCount, Boolean Global) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x0A);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt32BE(PrefetchSize)
            .WriteUInt16BE(PrefetchCount)
            .WriteBoolean(Global);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out BasicQos? result) {
        if (BufferExtensions.ReadUInt32BE(ref buffer, out var prefetchSize) &&
            BufferExtensions.ReadUInt16BE(ref buffer, out var prefetchCount) &&
            BufferExtensions.ReadBoolean(ref buffer, out var global))
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

readonly record struct BasicQosOk : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x0B);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) => writer;

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out BasicQosOk? result) {
        result  = new BasicQosOk();
        return true;
    }
}
