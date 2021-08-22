namespace Lapine.Client;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

public class RandomEndpointSelectionStrategy : IEndpointSelectionStrategy {
    public IEnumerable<IPEndPoint> GetConnectionSequence(IEnumerable<IPEndPoint> availableEndpoints) {
        var random = new Random();

        return availableEndpoints.OrderBy(_ => random.Next());
    }
}
