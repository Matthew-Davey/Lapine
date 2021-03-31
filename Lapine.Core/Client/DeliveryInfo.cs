namespace Lapine.Client {
    using System;

    public record DeliveryInfo(
        UInt64 DeliveryTag,
        Boolean Redelivered,
        String Exchange,
        String RoutingKey,
        UInt32 MessageCount
    );
}
