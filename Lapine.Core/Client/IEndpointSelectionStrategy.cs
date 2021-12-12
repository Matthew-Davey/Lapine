namespace Lapine.Client;

using System.Net;

public interface IEndpointSelectionStrategy {
    IEnumerable<IPEndPoint> GetConnectionSequence(IEnumerable<IPEndPoint> availableEndpoints);
}
