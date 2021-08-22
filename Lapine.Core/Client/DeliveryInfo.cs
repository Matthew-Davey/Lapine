namespace Lapine.Client;

using System;
using Lapine.Protocol.Commands;

public readonly record struct DeliveryInfo(
    UInt64 DeliveryTag,
    Boolean Redelivered,
    String Exchange,
    String? RoutingKey,
    UInt32? MessageCount
) {
    static internal DeliveryInfo FromBasicDeliver(BasicDeliver deliver) => new (
        DeliveryTag : deliver.DeliveryTag,
        Redelivered : deliver.Redelivered,
        Exchange    : deliver.ExchangeName,
        RoutingKey  : null,
        MessageCount: null
    );

    static internal DeliveryInfo FromBasicGetOk(BasicGetOk deliver) => new (
        DeliveryTag : deliver.DeliveryTag,
        Redelivered : deliver.Redelivered,
        Exchange    : deliver.ExchangeName,
        RoutingKey  : deliver.RoutingKey,
        MessageCount: deliver.MessageCount
    );
}
