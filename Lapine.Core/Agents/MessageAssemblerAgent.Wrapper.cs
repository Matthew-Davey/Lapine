namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

static partial class MessageAssemblerAgent {
    class Wrapper(IAgent<Protocol> agent) : IMessageAssemblerAgent {
        async Task<IObservable<(DeliveryInfo DeliveryInfo, BasicProperties Properties, MemoryBufferWriter<Byte> Buffer)>> IMessageAssemblerAgent.Begin(IObservable<RawFrame> frameStream, IConsumerAgent parent) {
            var reply = await agent.PostAndReplyAsync(replyChannel => new Begin(frameStream, replyChannel));
            return (IObservable<(DeliveryInfo DeliveryInfo, BasicProperties Properties, MemoryBufferWriter<Byte> Buffer)>) reply;
        }

        async Task IMessageAssemblerAgent.Stop() =>
            await agent.PostAsync(new Stop());
    }
}
