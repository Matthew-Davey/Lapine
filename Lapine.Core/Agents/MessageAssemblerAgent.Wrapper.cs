namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

static partial class MessageAssemblerAgent {
    class Wrapper(IAgent<Protocol> agent) : IMessageAssemblerAgent {
        async Task<IObservable<(DeliveryInfo DeliveryInfo, BasicProperties Properties, MemoryBufferWriter<Byte> Buffer)>> IMessageAssemblerAgent.Begin(IObservable<RawFrame> frameStream, IConsumerAgent parent) {
            return await agent.PostAndReplyAsync<IObservable<(DeliveryInfo DeliveryInfo, BasicProperties Properties, MemoryBufferWriter<Byte> Buffer)>>(replyChannel => new Begin(frameStream, replyChannel));
        }

        async Task IMessageAssemblerAgent.Stop() =>
            await agent.PostAsync(new Stop());
    }
}
