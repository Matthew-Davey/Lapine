namespace Lapine.Client;

public delegate Task MessageHandler(
    DeliveryInfo deliveryInfo,
    MessageProperties properties,
    ReadOnlyMemory<Byte> body
);
