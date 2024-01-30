namespace Lapine.Agents;

using System.Net;
using Lapine.Protocol;

abstract record ConnectionEvent;
record RemoteDisconnected(Exception Fault) : ConnectionEvent;

interface ISocketAgent {
    Task<(IObservable<ConnectionEvent> ConnectionEvents, IObservable<RawFrame> ReceivedFrames)> ConnectAsync(IPEndPoint endpoint, CancellationToken cancellationToken = default);
    Task Tune(UInt32 maxFrameSize);
    Task EnableTcpKeepAlives(TimeSpan probeTime, TimeSpan retryInterval, Int32 retryCount);
    Task Transmit(ISerializable entity);
    Task Disconnect();
    Task StopAsync();
}

static partial class SocketAgent {
    static public ISocketAgent Create() =>
        new Wrapper(Agent<Protocol>.StartNew(Disconnected()));
}
