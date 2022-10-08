namespace Lapine.Protocol;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

readonly record struct BasicProperties(
    String? ContentType,
    String? ContentEncoding,
    IReadOnlyDictionary<String, Object>? Headers,
    Byte? DeliveryMode,
    Byte? Priority,
    String? CorrelationId,
    String? ReplyTo,
    String? Expiration,
    String? MessageId,
    UInt64? Timestamp,
    String? Type,
    String? UserId,
    String? AppId,
    String? ClusterId
) : ISerializable {
    static public BasicProperties Empty => new (
        ContentType    : null,
        ContentEncoding: null,
        Headers        : null,
        DeliveryMode   : null,
        Priority       : null,
        CorrelationId  : null,
        ReplyTo        : null,
        Expiration     : null,
        MessageId      : null,
        Timestamp      : null,
        Type           : null,
        UserId         : null,
        AppId          : null,
        ClusterId      : null
    );

    PropertyFlags PropertyFlags => PropertyFlags.None
        | ContentType     switch { null => PropertyFlags.None, _ => PropertyFlags.ContentType }
        | ContentEncoding switch { null => PropertyFlags.None, _ => PropertyFlags.ContentEncoding }
        | Headers         switch { null => PropertyFlags.None, _ => PropertyFlags.Headers }
        | DeliveryMode    switch { null => PropertyFlags.None, _ => PropertyFlags.DeliveryMode }
        | Priority        switch { null => PropertyFlags.None, _ => PropertyFlags.Priority }
        | CorrelationId   switch { null => PropertyFlags.None, _ => PropertyFlags.CorrelationId }
        | ReplyTo         switch { null => PropertyFlags.None, _ => PropertyFlags.ReplyTo }
        | Expiration      switch { null => PropertyFlags.None, _ => PropertyFlags.Expiration }
        | MessageId       switch { null => PropertyFlags.None, _ => PropertyFlags.MessageId }
        | Timestamp       switch { null => PropertyFlags.None, _ => PropertyFlags.Timestamp }
        | Type            switch { null => PropertyFlags.None, _ => PropertyFlags.Type }
        | UserId          switch { null => PropertyFlags.None, _ => PropertyFlags.UserId }
        | AppId           switch { null => PropertyFlags.None, _ => PropertyFlags.AppId }
        | ClusterId       switch { null => PropertyFlags.None, _ => PropertyFlags.ClusterId };

    public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) {
        writer = writer.WriteUInt16BE((UInt16)PropertyFlags);

        writer = ContentType     switch { null => writer, var value => writer.WriteShortString(value) };
        writer = ContentEncoding switch { null => writer, var value => writer.WriteShortString(value) };
        writer = Headers         switch { null => writer, var value => writer.WriteFieldTable(value) };
        writer = DeliveryMode    switch { null => writer, var value => writer.WriteUInt8(value.Value) };
        writer = Priority        switch { null => writer, var value => writer.WriteUInt8(value.Value) };
        writer = CorrelationId   switch { null => writer, var value => writer.WriteShortString(value) };
        writer = ReplyTo         switch { null => writer, var value => writer.WriteShortString(value) };
        writer = Expiration      switch { null => writer, var value => writer.WriteShortString(value) };
        writer = MessageId       switch { null => writer, var value => writer.WriteShortString(value) };
        writer = Timestamp       switch { null => writer, var value => writer.WriteUInt64BE(value.Value) };
        writer = Type            switch { null => writer, var value => writer.WriteShortString(value) };
        writer = UserId          switch { null => writer, var value => writer.WriteShortString(value) };
        writer = AppId           switch { null => writer, var value => writer.WriteShortString(value) };
        writer = ClusterId       switch { null => writer, var value => writer.WriteShortString(value) };

        return writer;
    }

    static public Boolean Deserialize(ref ReadOnlyMemory<Byte> buffer, [NotNullWhen(true)] out BasicProperties? result) {
        if (BufferExtensions.ReadUInt16BE(ref buffer, out var propertyFlags)) {
            var flags = (PropertyFlags)propertyFlags;

            result = Empty;

            if (flags.HasFlag(PropertyFlags.ContentType)) {
                if (BufferExtensions.ReadShortString(ref buffer, out var contentType)) {
                    result = result.Value with { ContentType = contentType };
                }
                else {
                    result = default;
                    return false;
                }
            }

            if (flags.HasFlag(PropertyFlags.ContentEncoding)) {
                if (BufferExtensions.ReadShortString(ref buffer, out var contentEncoding)) {
                    result = result.Value with { ContentEncoding = contentEncoding };
                }
                else {
                    result = default;
                    return false;
                }
            }

            if (flags.HasFlag(PropertyFlags.Headers)) {
                if (BufferExtensions.ReadFieldTable(ref buffer, out var headers)) {
                    result = result.Value with { Headers = headers };
                }
                else {
                    result = default;
                    return false;
                }
            }

            if (flags.HasFlag(PropertyFlags.DeliveryMode)) {
                if (BufferExtensions.ReadUInt8(ref buffer, out var deliveryMode)) {
                    result = result.Value with { DeliveryMode = deliveryMode };
                }
                else {
                    result = default;
                    return false;
                }
            }

            if (flags.HasFlag(PropertyFlags.Priority)) {
                if (BufferExtensions.ReadUInt8(ref buffer, out var priority)) {
                    result = result.Value with { Priority = priority };
                }
                else {
                    result = default;
                    return false;
                }
            }

            if (flags.HasFlag(PropertyFlags.CorrelationId)) {
                if (BufferExtensions.ReadShortString(ref buffer, out var correlationId)) {
                    result = result.Value with { CorrelationId = correlationId };
                }
                else {
                    result = default;
                    return false;
                }
            }

            if (flags.HasFlag(PropertyFlags.ReplyTo)) {
                if (BufferExtensions.ReadShortString(ref buffer, out var replyTo)) {
                    result = result.Value with { ReplyTo = replyTo };
                }
                else {
                    result = default;
                    return false;
                }
            }

            if (flags.HasFlag(PropertyFlags.Expiration)) {
                if (BufferExtensions.ReadShortString(ref buffer, out var expiration)) {
                    result = result.Value with { Expiration = expiration };
                }
                else {
                    result = default;
                    return false;
                }
            }

            if (flags.HasFlag(PropertyFlags.MessageId)) {
                if (BufferExtensions.ReadShortString(ref buffer, out var messageId)) {
                    result = result.Value with { MessageId = messageId };
                }
                else {
                    result = default;
                    return false;
                }
            }

            if (flags.HasFlag(PropertyFlags.Timestamp)) {
                if (BufferExtensions.ReadUInt64BE(ref buffer, out var timestamp)) {
                    result = result.Value with { Timestamp = timestamp };
                }
                else {
                    result = default;
                    return false;
                }
            }

            if (flags.HasFlag(PropertyFlags.Type)) {
                if (BufferExtensions.ReadShortString(ref buffer, out var type)) {
                    result = result.Value with { Type = type };
                }
                else {
                    result = default;
                    return false;
                }
            }

            if (flags.HasFlag(PropertyFlags.UserId)) {
                if (BufferExtensions.ReadShortString(ref buffer, out var userId)) {
                    result = result.Value with { UserId = userId };
                }
                else {
                    result = default;
                    return false;
                }
            }

            if (flags.HasFlag(PropertyFlags.AppId)) {
                if (BufferExtensions.ReadShortString(ref buffer, out var appId)) {
                    result = result.Value with { AppId = appId };
                }
                else {
                    result = default;
                    return false;
                }
            }

            if (flags.HasFlag(PropertyFlags.ClusterId)) {
                if (BufferExtensions.ReadShortString(ref buffer, out var clusterId)) {
                    result = result.Value with { ClusterId = clusterId };
                }
                else {
                    result = default;
                    return false;
                }
            };

            return true;
        }
        else {
            result = default;
            return false;
        }
    }
}
