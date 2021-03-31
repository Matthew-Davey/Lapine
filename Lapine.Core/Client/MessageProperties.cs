namespace Lapine.Client {
    using System;
    using System.Collections.Immutable;
    using Lapine.Protocol;

    public record MessageProperties(
        String? ContentType,
        String? ContentEncoding,
        IImmutableDictionary<String, Object>? Headers,
        DeliveryMode? DeliveryMode,
        Byte? Priority,
        String? CorrelationId,
        String? ReplyTo,
        TimeSpan? Expiration,
        String? MessageId,
        DateTimeOffset? Timestamp,
        String? Type,
        String? UserId,
        String? AppId,
        String? ClusterId
    ) {
        static public MessageProperties Empty => new (
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

        static internal MessageProperties FromBasicProperties(BasicProperties properties) => Empty with {
            ContentType     = properties.ContentType,
            ContentEncoding = properties.ContentEncoding,
            Headers         = properties.Headers?.ToImmutableDictionary(),
            DeliveryMode    = (DeliveryMode?)properties.DeliveryMode,
            Priority        = properties.Priority,
            CorrelationId   = properties.CorrelationId,
            ReplyTo         = properties.ReplyTo,
            Expiration      = properties.Expiration switch {
                null         => null,
                String value => TimeSpan.Parse(value)
            },
            MessageId       = properties.MessageId,
            Timestamp       = properties.Timestamp switch {
                null         => null,
                UInt64 value => DateTimeOffset.FromUnixTimeSeconds((Int64)value)
            },
            Type            = properties.Type,
            UserId          = properties.UserId,
            AppId           = properties.AppId,
            ClusterId       = properties.ClusterId
        };

        internal BasicProperties ToBasicProperties() => BasicProperties.Empty with {
            ContentType     = ContentType,
            ContentEncoding = ContentEncoding,
            Headers         = Headers,
            DeliveryMode    = DeliveryMode switch {
                null               => (Byte?)null,
                DeliveryMode value => (Byte?)value
            },
            Priority        = Priority,
            CorrelationId   = CorrelationId,
            ReplyTo         = ReplyTo,
            Expiration      = Expiration switch {
                null           => (String?)null,
                TimeSpan value => $"{value.TotalMilliseconds:F0}"
            },
            MessageId       = MessageId,
            Timestamp       = Timestamp switch {
                null                 => (UInt64?)null,
                DateTimeOffset value => (UInt64)value.ToUnixTimeSeconds()
            },
            Type            = Type,
            UserId          = UserId,
            AppId           = AppId,
            ClusterId       = ClusterId
        };
    }
}
