namespace Lapine.Protocol.Commands;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using static System.String;

record struct ConnectionStart((Byte Major, Byte Minor) Version, IReadOnlyDictionary<String, Object> ServerProperties, IList<String> Mechanisms, IList<String> Locales) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x0A);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteUInt8(Version.Major)
            .WriteUInt8(Version.Minor)
            .WriteFieldTable(ServerProperties)
            .WriteLongString(Join(' ', Mechanisms))
            .WriteLongString(Join(' ', Locales));

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConnectionStart? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadUInt8(out var major, out surplus) &&
            surplus.ReadUInt8(out var minor, out surplus) &&
            surplus.ReadFieldTable(out var serverProperties, out surplus) &&
            surplus.ReadLongString(out var mechanisms, out surplus) &&
            surplus.ReadLongString(out var locales, out surplus))
        {
            result = new ConnectionStart((major, minor), serverProperties, mechanisms.Split(' '), locales.Split(' '));
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}

record struct ConnectionStartOk(IReadOnlyDictionary<String, Object> PeerProperties, String Mechanism, String Response, String Locale) : ICommand {
    public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x0B);

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
        writer.WriteFieldTable(PeerProperties)
            .WriteShortString(Mechanism)
            .WriteLongString(Response)
            .WriteShortString(Locale);

    static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out ConnectionStartOk? result, out ReadOnlySpan<Byte> surplus) {
        if (buffer.ReadFieldTable(out var peerProperties, out surplus) &&
            surplus.ReadShortString(out var mechanism, out surplus) &&
            surplus.ReadLongString(out var response, out surplus) &&
            surplus.ReadShortString(out var locale, out surplus))
        {
            result = new ConnectionStartOk(peerProperties, mechanism, response, locale);
            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
