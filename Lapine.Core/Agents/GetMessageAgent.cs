namespace Lapine.Agents;

using Lapine.Client;
using Lapine.Protocol;

abstract record GetMessageResult;
record NoMessage : GetMessageResult;
record Message(DeliveryInfo DeliveryInfo, BasicProperties Properties, ReadOnlyMemory<Byte> Body) : GetMessageResult;

interface IGetMessageAgent {
    Task<GetMessageResult> GetMessages(String queue, Acknowledgements acknowledgements);
}

static partial class GetMessageAgent {
    static public IGetMessageAgent Create(IObservable<RawFrame> receivedFrames, IDispatcherAgent dispatcher, CancellationToken cancellationToken) =>
        new Wrapper(Agent<Protocol>.StartNew(AwaitingGetMessages(receivedFrames, dispatcher, cancellationToken)));
}
