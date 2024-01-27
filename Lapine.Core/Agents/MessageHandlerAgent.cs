namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

interface IMessageHandlerAgent {
    Task HandleMessage(IDispatcherAgent dispatcher, ConsumerConfiguration consumerConfiguration, DeliveryInfo deliveryInfo, BasicProperties properties, MemoryBufferWriter<Byte> buffer);
    Task Stop();
}

class MessageHandlerAgent : IMessageHandlerAgent {
    readonly IAgent<Protocol> _agent;

    MessageHandlerAgent(IAgent<Protocol> agent) =>
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));

    static public IMessageHandlerAgent Create(IConsumerAgent parent) =>
        new MessageHandlerAgent(Agent<Protocol>.StartNew(Main(parent)));

    abstract record Protocol;
    record HandleMessage(
        IMessageHandlerAgent Self,
        IDispatcherAgent Dispatcher,
        ConsumerConfiguration ConsumerConfiguration,
        DeliveryInfo Delivery,
        BasicProperties Properties,
        MemoryBufferWriter<Byte> Buffer
    ) : Protocol;

    static Behaviour<Protocol> Main(IConsumerAgent parent) => async context => {
        switch (context.Message) {
            case HandleMessage(var self, var dispatcher, var consumerConfiguration, var deliveryInfo, var properties, var buffer): {
                try {
                    await consumerConfiguration.Handler(
                        deliveryInfo: deliveryInfo,
                        properties  : MessageProperties.FromBasicProperties(properties),
                        body        : buffer.WrittenMemory
                    );
                    await dispatcher.Dispatch(new BasicAck(
                        DeliveryTag: deliveryInfo.DeliveryTag,
                        Multiple   : false
                    ));
                }
                catch (MessageException) {
                    // nack without requeue...
                    await dispatcher.Dispatch(new BasicReject(
                        DeliveryTag: deliveryInfo.DeliveryTag,
                        ReQueue    : false
                    ));
                }
                catch (ConsumerException) {
                    // nack with requeue...
                    await dispatcher.Dispatch(new BasicReject(
                        DeliveryTag: deliveryInfo.DeliveryTag,
                        ReQueue    : true
                    ));
                }
                finally {
                    // Release the buffer containing the message body back into the memory pool...
                    buffer.Dispose();

                    // Tell consumer agent we're ready to handle another message...
                    await parent.HandlerReady(self);
                }

                return context;
            }
            default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Main)}' behaviour.");
        }
    };

    async Task IMessageHandlerAgent.HandleMessage(IDispatcherAgent dispatcher, ConsumerConfiguration consumerConfiguration, DeliveryInfo deliveryInfo, BasicProperties properties, MemoryBufferWriter<Byte> buffer) =>
        await _agent.PostAsync(new HandleMessage(this, dispatcher, consumerConfiguration, deliveryInfo, properties, buffer));

    async Task IMessageHandlerAgent.Stop() =>
        await _agent.StopAsync();
}
