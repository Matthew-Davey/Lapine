namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct ConfirmSelect(Boolean NoWait) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x55, 0x0A);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteBoolean(NoWait);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConfirmSelect? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadBoolean(out var noWait, out surplus)) {
            result = new ConfirmSelect(noWait);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

readonly record struct ConfirmSelectOk() : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x55, 0x0B);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer;

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConfirmSelectOk? result, out ReadOnlySpan<Byte> surplus) {
        surplus = buffer;
        result = new ConfirmSelectOk();
        return true;
    }
}
