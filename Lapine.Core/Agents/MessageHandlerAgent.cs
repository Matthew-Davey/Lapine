namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

interface IMessageHandlerAgent {
    Task HandleMessage(IDispatcherAgent dispatcher, ConsumerConfiguration consumerConfiguration, DeliveryInfo deliveryInfo, BasicProperties properties, MemoryBufferWriter<Byte> buffer);
    Task Stop();
}

static partial class MessageHandlerAgent {
    static public IMessageHandlerAgent Create(IConsumerAgent parent) =>
        new Wrapper(Agent<Protocol>.StartNew(Main(parent)));
}
