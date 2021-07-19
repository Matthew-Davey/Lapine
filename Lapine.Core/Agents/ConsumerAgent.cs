namespace Lapine.Agents {
    using System;
    using System.Buffers;
    using System.Threading.Tasks;
    using Lapine.Client;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.ConsumerAgent.Protocol;
    using static Lapine.Agents.DispatcherAgent.Protocol;

    static class ConsumerAgent {
        static public class Protocol {
            public record Start(String ConsumerTag, MessageHandler Handler, PID Dispatcher);
            public record HandleMessage(DeliveryInfo Delivery, BasicProperties Properties, ReadOnlyMemory<Byte> Body, IMemoryOwner<Byte> Buffer);
            public record HandleEmptyMessage(DeliveryInfo Delivery, BasicProperties Properties);
        }

        static public Props Create() =>
            Props.FromProducer(() => new Actor());

        class Actor : IActor {
            readonly Behavior _behaviour;

            public Actor() =>
                _behaviour = new Behavior(Unstarted);

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Unstarted(IContext context) {
                switch (context.Message) {
                    case Start start: {
                        _behaviour.BecomeStacked(Consuming(start.ConsumerTag, start.Handler, start.Dispatcher));
                        break;
                    }
                }
                return CompletedTask;
            }

            static Receive Consuming(String consumerTag, MessageHandler handler, PID dispatcher) =>
                async (IContext context) => {
                    switch (context.Message) {
                        case HandleMessage handle: {
                            try {
                                await handler(handle.Delivery, MessageProperties.FromBasicProperties(handle.Properties), handle.Body);
                                context.Send(dispatcher, Dispatch.Command(new BasicAck(handle.Delivery.DeliveryTag, false)));
                            }
                            catch (MessageException) {
                                // nack without requeue...
                                context.Send(dispatcher, Dispatch.Command(new BasicReject(handle.Delivery.DeliveryTag, false)));
                            }
                            catch (ConsumerException) {
                                // nack with requeue...
                                context.Send(dispatcher, Dispatch.Command(new BasicReject(handle.Delivery.DeliveryTag, true)));
                            }
                            finally {
                                // Release the buffer containing the message body back into the memory pool...
                                handle.Buffer.Dispose();
                            }
                            break;
                        }
                        case HandleEmptyMessage handle: {
                            try {
                                await handler(handle.Delivery, MessageProperties.FromBasicProperties(handle.Properties), Memory<Byte>.Empty);
                                context.Send(dispatcher, Dispatch.Command(new BasicAck(handle.Delivery.DeliveryTag, false)));
                            }
                            catch (MessageException) {
                                // nack without requeue...
                                context.Send(dispatcher, Dispatch.Command(new BasicReject(handle.Delivery.DeliveryTag, false)));
                            }
                            catch (ConsumerException) {
                                // nack with requeue...
                                context.Send(dispatcher, Dispatch.Command(new BasicReject(handle.Delivery.DeliveryTag, true)));
                            }
                            break;
                        }
                        case Stopping _: {
                            context.Send(dispatcher, Dispatch.Command(new BasicCancel(consumerTag, false)));
                            break;
                        }
                    }
                };
        }
    }
}
