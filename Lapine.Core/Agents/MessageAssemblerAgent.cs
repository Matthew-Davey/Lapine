namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

interface IMessageAssemblerAgent {
    Task<IObservable<(DeliveryInfo DeliveryInfo, BasicProperties Properties, MemoryBufferWriter<Byte> Buffer)>> Begin(IObservable<RawFrame> frameStream, IConsumerAgent parent);
    Task Stop();
}

static partial class MessageAssemblerAgent {
    static public IMessageAssemblerAgent StartNew() =>
        new Wrapper(Agent<Protocol>.StartNew(Unstarted()));
}
