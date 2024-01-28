namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

static partial class MessageHandlerAgent {
    class Wrapper(IAgent<Protocol> agent) : IMessageHandlerAgent {
        async Task IMessageHandlerAgent.HandleMessage(IDispatcherAgent dispatcher, ConsumerConfiguration consumerConfiguration, DeliveryInfo deliveryInfo, BasicProperties properties, MemoryBufferWriter<Byte> buffer) =>
            await agent.PostAsync(new HandleMessage(this, dispatcher, consumerConfiguration, deliveryInfo, properties, buffer));

        async Task IMessageHandlerAgent.Stop() =>
            await agent.StopAsync();
    }
}
