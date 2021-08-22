namespace Lapine.Protocol;

using System;

[Flags]
enum PropertyFlags : UInt16 {
    None            = 0b0000000000000000,
    ContentType     = 0b1000000000000000,
    ContentEncoding = 0b0100000000000000,
    Headers         = 0b0010000000000000,
    DeliveryMode    = 0b0001000000000000,
    Priority        = 0b0000100000000000,
    CorrelationId   = 0b0000010000000000,
    ReplyTo         = 0b0000001000000000,
    Expiration      = 0b0000000100000000,
    MessageId       = 0b0000000010000000,
    Timestamp       = 0b0000000001000000,
    Type            = 0b0000000000100000,
    UserId          = 0b0000000000010000,
    AppId           = 0b0000000000001000,
    ClusterId       = 0b0000000000000100
}
