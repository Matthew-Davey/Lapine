namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;

using static Lapine.Agents.MessageHandlerAgent.Protocol;
using static Lapine.Agents.DispatcherAgent.Protocol;

static class MessageHandlerAgent {
    static public class Protocol {
        public record HandleMessage(
            IAgent Dispatcher,
            ConsumerConfiguration ConsumerConfiguration,
            DeliveryInfo Delivery,
            BasicProperties Properties,
            MemoryBufferWriter<Byte> Buffer,
            Action<HandlerReady> PostToParent
        );
        public record HandlerReady(IAgent Handler);
    }

    static public IAgent Create() =>
        Agent.StartNew(Main());

    static Behaviour Main() => async context => {
        switch (context.Message) {
            case Started: {
                return context;
            }
            case HandleMessage(var dispatcher, var consumerConfiguration, var deliveryInfo, var properties, var buffer, var postToParent): {
                try {
                    await consumerConfiguration.Handler(
                        deliveryInfo: deliveryInfo,
                        properties  : MessageProperties.FromBasicProperties(properties),
                        body        : buffer.WrittenMemory
                    );
                    await dispatcher.PostAsync(Dispatch.Command(new BasicAck(
                        DeliveryTag: deliveryInfo.DeliveryTag,
                        Multiple   : false
                    )));
                }
                catch (MessageException) {
                    // nack without requeue...
                    await dispatcher.PostAsync(Dispatch.Command(new BasicReject(
                        DeliveryTag: deliveryInfo.DeliveryTag,
                        ReQueue    : false
                    )));
                }
                catch (ConsumerException) {
                    // nack with requeue...
                    await dispatcher.PostAsync(Dispatch.Command(new BasicReject(
                        DeliveryTag: deliveryInfo.DeliveryTag,
                        ReQueue    : true
                    )));
                }
                finally {
                    // Release the buffer containing the message body back into the memory pool...
                    buffer.Dispose();

                    // Tell consumer agent we're ready to handle another message...
                    postToParent(new HandlerReady(context.Self));
                }

                return context;
            }
            default: throw new Exception($"Unexpected message '{context.Message.GetType().FullName}' in '{nameof(Main)}' behaviour.");
        }
    };
}
