namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

static partial class MessageHandlerAgent {
    abstract record Protocol;

    record HandleMessage(
        IMessageHandlerAgent Self,
        IDispatcherAgent Dispatcher,
        ConsumerConfiguration ConsumerConfiguration,
        DeliveryInfo Delivery,
        BasicProperties Properties,
        MemoryBufferWriter<Byte> Buffer
    ) : Protocol;
}
