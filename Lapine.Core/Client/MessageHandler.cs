namespace Lapine.Client;

using System;
using System.Threading.Tasks;

public delegate Task MessageHandler(
    DeliveryInfo deliveryInfo,
    MessageProperties properties,
    ReadOnlyMemory<Byte> body
);
