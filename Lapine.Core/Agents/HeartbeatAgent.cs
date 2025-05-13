namespace Lapine.Agents;

using Lapine.Protocol;

abstract record HeartbeatEvent;
record RemoteFlatline : HeartbeatEvent;

interface IHeartbeatAgent {
    Task<IObservable<HeartbeatEvent>> Start(IObservable<RawFrame> frameStream, IDispatcherAgent dispatcher, TimeSpan frequency);
    Task Stop();
}

static partial class HeartbeatAgent {
    static public IHeartbeatAgent Create() =>
        new Wrapper(Agent<Protocol>.StartNew(Idle()));
}
