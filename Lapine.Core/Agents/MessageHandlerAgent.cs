namespace Lapine.Agents;

using System;
using System.Threading.Tasks;
using Lapine.Client;
using Lapine.Protocol;
using Lapine.Protocol.Commands;
using Proto;

using static Lapine.Agents.MessageHandlerAgent.Protocol;
using static Lapine.Agents.DispatcherAgent.Protocol;

static class MessageHandlerAgent {
    static public class Protocol {
        public record HandleMessage(
            PID Dispatcher,
            ConsumerConfiguration ConsumerConfiguration,
            DeliveryInfo Delivery,
            BasicProperties Properties,
            MemoryBufferWriter<Byte> Buffer
        );
        public record HandlerReady(PID Handler);
    }

    static public Props Create() =>
        Props.FromProducer(() => new Actor());

    class Actor : IActor {
        public async Task ReceiveAsync(IContext context) {
            switch (context.Message) {
                case HandleMessage handle: {
                    try {
                        await handle.ConsumerConfiguration.Handler(
                            deliveryInfo: handle.Delivery,
                            properties  : MessageProperties.FromBasicProperties(handle.Properties),
                            body        : handle.Buffer.WrittenMemory
                        );
                        context.Send(handle.Dispatcher, Dispatch.Command(new BasicAck(
                            DeliveryTag: handle.Delivery.DeliveryTag,
                            Multiple   : false
                        )));
                        context.Send(context.Parent!, new HandlerReady(context.Self!));
                    }
                    catch (MessageException) {
                        // nack without requeue...
                        context.Send(handle.Dispatcher, Dispatch.Command(new BasicReject(
                            DeliveryTag: handle.Delivery.DeliveryTag,
                            ReQueue    : false
                        )));
                    }
                    catch (ConsumerException) {
                        // nack with requeue...
                        context.Send(handle.Dispatcher, Dispatch.Command(new BasicReject(
                            DeliveryTag: handle.Delivery.DeliveryTag,
                            ReQueue    : true
                        )));
                    }
                    finally {
                        // Release the buffer containing the message body back into the memory pool...
                        handle.Buffer.Dispose();
                    }
                    break;
                }
            }
        }
    }
}
