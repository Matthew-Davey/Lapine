namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol.Commands;

static partial class MessageHandlerAgent {
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
}
