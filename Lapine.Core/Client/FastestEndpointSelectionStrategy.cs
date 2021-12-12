namespace Lapine.Client;

using System.Net;
using System.Net.NetworkInformation;

public class FastestEndpointSelectionStrategy : IEndpointSelectionStrategy {
    public const UInt16 DefaultPingTimeout = 3000;

    public UInt16 PingTimeout { get; }

    readonly Ping _ping = new ();

    public FastestEndpointSelectionStrategy(UInt16 pingTimeout = DefaultPingTimeout) =>
        PingTimeout = pingTimeout;

    public IEnumerable<IPEndPoint> GetConnectionSequence(IEnumerable<IPEndPoint> availableEndpoints) =>
        from endpoint in availableEndpoints.AsParallel()
        let reply = _ping.Send(endpoint.Address, PingTimeout)
        where reply.Status is IPStatus.Success
        orderby reply.RoundtripTime
        select endpoint;
}
