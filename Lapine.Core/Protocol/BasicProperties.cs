namespace Lapine.Protocol {
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    public record BasicProperties(
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

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, [NotNullWhen(true)] out BasicProperties? result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadUInt16BE(out var propertyFlags, out surplus)) {
                var flags = (PropertyFlags)propertyFlags;

                result = Empty;

                if (flags.HasFlag(PropertyFlags.ContentType)) {
                    if (surplus.ReadShortString(out var contentType, out surplus)) {
                        result = result with { ContentType = contentType };
                    }
                    else {
                        result = default;
                        return false;
                    }
                }

                if (flags.HasFlag(PropertyFlags.ContentEncoding)) {
                    if (surplus.ReadShortString(out var contentEncoding, out surplus)) {
                        result = result with { ContentEncoding = contentEncoding };
                    }
                    else {
                        result = default;
                        return false;
                    }
                }

                if (flags.HasFlag(PropertyFlags.Headers)) {
                    if (surplus.ReadFieldTable(out var headers, out surplus)) {
                        result = result with { Headers = headers };
                    }
                    else {
                        result = default;
                        return false;
                    }
                }

                if (flags.HasFlag(PropertyFlags.DeliveryMode)) {
                    if (surplus.ReadUInt8(out var deliveryMode, out surplus)) {
                        result = result with { DeliveryMode = deliveryMode };
                    }
                    else {
                        result = default;
                        return false;
                    }
                }

                if (flags.HasFlag(PropertyFlags.Priority)) {
                    if (surplus.ReadUInt8(out var priority, out surplus)) {
                        result = result with { Priority = priority };
                    }
                    else {
                        result = default;
                        return false;
                    }
                }

                if (flags.HasFlag(PropertyFlags.CorrelationId)) {
                    if (surplus.ReadShortString(out var correlationId, out surplus)) {
                        result = result with { CorrelationId = correlationId };
                    }
                    else {
                        result = default;
                        return false;
                    }
                }

                if (flags.HasFlag(PropertyFlags.ReplyTo)) {
                    if (surplus.ReadShortString(out var replyTo, out surplus)) {
                        result = result with { ReplyTo = replyTo };
                    }
                    else {
                        result = default;
                        return false;
                    }
                }

                if (flags.HasFlag(PropertyFlags.Expiration)) {
                    if (surplus.ReadShortString(out var expiration, out surplus)) {
                        result = result with { Expiration = expiration };
                    }
                    else {
                        result = default;
                        return false;
                    }
                }

                if (flags.HasFlag(PropertyFlags.MessageId)) {
                    if (surplus.ReadShortString(out var messageId, out surplus)) {
                        result = result with { MessageId = messageId };
                    }
                    else {
                        result = default;
                        return false;
                    }
                }

                if (flags.HasFlag(PropertyFlags.Timestamp)) {
                    if (surplus.ReadUInt64BE(out var timestamp, out surplus)) {
                        result = result with { Timestamp = timestamp };
                    }
                    else {
                        result = default;
                        return false;
                    }
                }

                if (flags.HasFlag(PropertyFlags.Type)) {
                    if (surplus.ReadShortString(out var type, out surplus)) {
                        result = result with { Type = type };
                    }
                    else {
                        result = default;
                        return false;
                    }
                }

                if (flags.HasFlag(PropertyFlags.UserId)) {
                    if (surplus.ReadShortString(out var userId, out surplus)) {
                        result = result with { UserId = userId };
                    }
                    else {
                        result = default;
                        return false;
                    }
                }

                if (flags.HasFlag(PropertyFlags.AppId)) {
                    if (surplus.ReadShortString(out var appId, out surplus)) {
                        result = result with { AppId = appId };
                    }
                    else {
                        result = default;
                        return false;
                    }
                }

                if (flags.HasFlag(PropertyFlags.ClusterId)) {
                    if (surplus.ReadShortString(out var clusterId, out surplus)) {
                        result = result with { ClusterId = clusterId };
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
}
