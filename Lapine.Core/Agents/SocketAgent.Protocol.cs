namespace Lapine.Agents;

using System.Net;

static partial class SocketAgent {
    abstract record Protocol;
    record Connect(IPEndPoint Endpoint, AsyncReplyChannel ReplyChannel, CancellationToken CancellationToken = default) : Protocol;
    record Tune(UInt32 MaxFrameSize) : Protocol;
    record EnableTcpKeepAlives(TimeSpan ProbeTime, TimeSpan RetryInterval, Int32 RetryCount) : Protocol;
    record Transmit(ISerializable Entity) : Protocol;
    record Disconnect : Protocol;
    record Poll : Protocol;
    record OnAsyncResult(IAsyncResult Result) : Protocol;
}
