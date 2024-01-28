namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

static partial class ConsumerAgent {
    abstract record Protocol;

    record StartConsuming(
        IConsumerAgent Self,
        String ConsumerTag,
        IObservable<RawFrame> ReceivedFrames,
        IDispatcherAgent Dispatcher,
        String Queue,
        ConsumerConfiguration ConsumerConfiguration,
        IReadOnlyDictionary<String, Object>? Arguments,
        AsyncReplyChannel ReplyChannel
    ) : Protocol;

    record ConsumeMessage(
        DeliveryInfo Delivery,
        BasicProperties Properties,
        MemoryBufferWriter<Byte> Buffer
    ) : Protocol;

    record HandlerReady(
        IMessageHandlerAgent Handler
    ) : Protocol;

    record Stop : Protocol;
}
