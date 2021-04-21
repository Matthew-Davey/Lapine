namespace Lapine.Client {
    using System.Collections.Generic;
    using System.Net;

    // In general `RandomEndpointSelectionStrategy` should be preferred over this one. However this is useful for testing the
    // connection process.
    public class InOrderEndpointSelectionStrategy : IEndpointSelectionStrategy {
        public IEnumerable<IPEndPoint> GetConnectionSequence(IEnumerable<IPEndPoint> availableEndpoints) =>
            availableEndpoints;
    }
}
