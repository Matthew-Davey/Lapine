namespace Lapine.Client;

using System.Net;

public class RandomEndpointSelectionStrategy : IEndpointSelectionStrategy {
    public IEnumerable<IPEndPoint> GetConnectionSequence(IEnumerable<IPEndPoint> availableEndpoints) {
        var random = new Random();

        return availableEndpoints.OrderBy(_ => random.Next());
    }
}
