namespace Lapine.Protocol.Commands;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct ConnectionSecure(String Challenge) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x14);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteLongString(Challenge);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ConnectionSecure? result) {
        if (BufferExtensions.ReadLongString(ref buffer, out var challenge)) {
            result = new ConnectionSecure(challenge);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

readonly record struct ConnectionSecureOk(String Response) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x15);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteLongString(Response);

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out ConnectionSecureOk? result) {
        if (BufferExtensions.ReadLongString(ref buffer, out var response)) {
            result = new ConnectionSecureOk(response);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
